// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Profiling;
using Hangfire.Storage;

namespace Hangfire.Server
{
    /// <summary>
    /// Represents a background process responsible for <i>enqueueing recurring 
    /// jobs</i>.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>This background process polls the <i>recurring job schedule</i>
    /// for recurring jobs ready to be enqueued. Interval between scheduler
    /// polls is hard-coded to <b>1 minute</b> as a compromise between
    /// frequency and additional stress on job storage.</para>
    /// 
    /// <note type="tip">
    /// Use custom background processes if you need to schedule recurring jobs
    /// with frequency less than one minute. Please see the 
    /// <see cref="IBackgroundProcess"/> interface for details.
    /// </note>
    /// 
    /// <para>Recurring job schedule is based on Set and Hash data structures
    /// of a job storage, so you can use this background process as an example 
    /// of a custom extension.</para>
    /// 
    /// <para>Multiple instances of this background process can be used in
    /// separate threads/processes without additional configuration (distributed
    /// locks are used). However, this only adds support for fail-over, and does 
    /// not increase the performance.</para>
    /// 
    /// <note type="important">
    /// If you are using <b>custom filter providers</b>, you need to pass a 
    /// custom <see cref="IBackgroundJobFactory"/> instance to make this 
    /// process respect your filters when enqueueing background jobs.
    /// </note>
    /// </remarks>
    /// 
    /// <threadsafety static="true" instance="true"/>
    /// 
    /// <seealso cref="RecurringJobManager"/>
    public class RecurringJobScheduler : IBackgroundProcess
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);
        private static readonly int BatchSize = 1000;
        private static readonly int MaxRetryAttemptCount = 5;
        private static readonly int MaxSupportedVersion = 2;

        private readonly ILog _logger = LogProvider.For<RecurringJobScheduler>();
        private readonly ConcurrentDictionary<Type, bool> _isBatchingAvailableCache = new ConcurrentDictionary<Type, bool>();

        private readonly IBackgroundJobFactory _factory;
        private readonly Func<DateTime> _nowFactory;
        private readonly ITimeZoneResolver _timeZoneResolver;
        private readonly TimeSpan _pollingDelay;
        private readonly IProfiler _profiler;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobScheduler"/>
        /// class with default background job factory.
        /// </summary>
        public RecurringJobScheduler()
            : this(new BackgroundJobFactory(JobFilterProviders.Providers))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobScheduler"/>
        /// class with custom background job factory and a state machine.
        /// </summary>
        /// <param name="factory">Factory that will be used to create background jobs.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        public RecurringJobScheduler(
            [NotNull] IBackgroundJobFactory factory)
            : this(factory, TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobScheduler"/> class
        /// with custom background job factory, state machine and clocks.
        /// </summary>
        /// <param name="factory">Factory that will be used to create background jobs.</param>
        /// <param name="pollingDelay">Delay before another polling attempt, when no jobs scheduled yet.</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        public RecurringJobScheduler(
            [NotNull] IBackgroundJobFactory factory,
            TimeSpan pollingDelay)
            : this(factory, pollingDelay, new DefaultTimeZoneResolver())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobScheduler"/> class
        /// with custom background job factory, state machine and clocks.
        /// </summary>
        /// <param name="factory">Factory that will be used to create background jobs.</param>
        /// <param name="pollingDelay">Delay before another polling attempt, when no jobs scheduled yet.</param>
        /// <param name="timeZoneResolver">Function that returns a time zone object by its identifier.</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="timeZoneResolver"/> is null.</exception>
        public RecurringJobScheduler(
            [NotNull] IBackgroundJobFactory factory,
            TimeSpan pollingDelay,
            [NotNull] ITimeZoneResolver timeZoneResolver)
            : this(factory, pollingDelay, timeZoneResolver, () => DateTime.UtcNow)
        {
        }

        public RecurringJobScheduler(
            [NotNull] IBackgroundJobFactory factory,
            TimeSpan pollingDelay,
            [NotNull] ITimeZoneResolver timeZoneResolver,
            [NotNull] Func<DateTime> nowFactory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (nowFactory == null) throw new ArgumentNullException(nameof(nowFactory));
            if (timeZoneResolver == null) throw new ArgumentNullException(nameof(timeZoneResolver));

            _factory = factory;
            _nowFactory = nowFactory;
            _timeZoneResolver = timeZoneResolver;
            _pollingDelay = pollingDelay;
            _profiler = new SlowLogProfiler(_logger);
        }

        /// <inheritdoc />
        public void Execute(BackgroundProcessContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            int jobsProcessed;

            do
            {
                jobsProcessed = EnqueueNextRecurringJobs(context);

                if (jobsProcessed != 0)
                {
                    _logger.Debug($"{jobsProcessed} recurring job(s) processed by scheduler.");
                }
            } while (jobsProcessed > 0 && !context.IsStopping);

            if (_pollingDelay > TimeSpan.Zero)
            {
                context.Wait(_pollingDelay);
            }
            else
            {
                var now = _nowFactory();
                context.Wait(now.AddMilliseconds(-now.Millisecond).AddSeconds(-now.Second).AddMinutes(1) - now);
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GetType().Name;
        }

        private int EnqueueNextRecurringJobs(BackgroundProcessContext context)
        {
            return UseConnectionDistributedLock(context.Storage, connection =>
            {
                var jobsProcessed = 0;

                if (IsBatchingAvailable(connection))
                {
                    var now = _nowFactory();
                    var timestamp = JobHelper.ToTimestamp(now);
                    var recurringJobIds = ((JobStorageConnection)connection).GetFirstByLowestScoreFromSet("recurring-jobs", 0, timestamp, BatchSize);

                    if (recurringJobIds != null)
                    {
                        foreach (var recurringJobId in recurringJobIds)
                        {
                            if (context.IsStopping) break;

                            TryEnqueueBackgroundJob(context, connection, recurringJobId, now);
                            jobsProcessed++;
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < BatchSize; i++)
                    {
                        if (context.IsStopping) break;

                        var now = _nowFactory();
                        var timestamp = JobHelper.ToTimestamp(now);

                        var recurringJobId = connection.GetFirstByLowestScoreFromSet("recurring-jobs", 0, timestamp);
                        if (recurringJobId == null) break;

                        TryEnqueueBackgroundJob(context, connection, recurringJobId, now);
                        jobsProcessed++;
                    }
                }

                return jobsProcessed;
            });
        }

        private void TryEnqueueBackgroundJob(
            BackgroundProcessContext context,
            IStorageConnection connection,
            string recurringJobId,
            DateTime now)
        {
            using (connection.AcquireDistributedRecurringJobLock(recurringJobId, LockTimeout))
            {
                var recurringJob = connection.GetRecurringJob(recurringJobId);

                if (recurringJob == null)
                {
                    RemoveRecurringJob(connection, recurringJobId);
                    return;
                }

                ScheduleRecurringJob(context, connection, recurringJobId, recurringJob, now);
            }
        }

        private void ScheduleRecurringJob(BackgroundProcessContext context, IStorageConnection connection,
            string recurringJobId, RecurringJobEntity recurringJob, DateTime now)
        {
            // We always start a transaction, regardless our recurring job was updated or not,
            // to prevent from infinite loop, when there's an old processing server (pre-1.7.0)
            // in our environment that doesn't know it should modify the score for entries in
            // the recurring jobs set.
            using (var transaction = connection.CreateWriteTransaction())
            {
                Exception exception = null;
                
                try
                {
                    // We can't handle recurring job with unsupported versions - there may be additional
                    // features. we don't know about. We also shouldn't stop the whole scheduler as
                    // there may be jobs with lower versions. Instead, we'll re-schedule such a job and
                    // emit a warning message to the log.
                    if (recurringJob.Version.HasValue && recurringJob.Version > MaxSupportedVersion)
                    {
                        throw new NotSupportedException($"Server '{context.ServerId}' can't process recurring job '{recurringJobId}' of version '{recurringJob.Version ?? 1}'. Max supported version of this server is '{MaxSupportedVersion}'.");
                    }
                    
                    var backgroundJobs = new List<BackgroundJob>();
                    var precision = _pollingDelay + _pollingDelay;

                    var executions = recurringJob.ScheduleNext(
                        _timeZoneResolver,
                        recurringJob.LastExecution ?? recurringJob.CreatedAt?.AddSeconds(-1) ?? now.AddSeconds(-1),
                        now,
                        precision);

                    foreach (var execution in executions)
                    {
                        var backgroundJob = _factory.TriggerRecurringJob(
                            context.Storage,
                            connection,
                            _profiler,
                            recurringJob,
                            execution);

                        if (!String.IsNullOrEmpty(backgroundJob?.Id))
                        {
                            backgroundJobs.Add(backgroundJob);
                        }
                        else
                        {
                            _logger.Debug($"Recurring job '{recurringJobId}' execution at '{execution}' has been canceled.");
                        }
                    }

                    foreach (var backgroundJob in backgroundJobs)
                    {
                        _factory.StateMachine.EnqueueBackgroundJob(
                            context.Storage,
                            connection,
                            transaction,
                            recurringJob,
                            backgroundJob,
                            "Triggered by recurring job scheduler",
                            _profiler);
                    }

                    recurringJob.RetryAttempt = 0;
                }
                catch (Exception ex) when (ex.IsCatchableExceptionType())
                {
                    exception = ex;
                }

                if (exception != null)
                {
                    RetryRecurringJob(recurringJobId, recurringJob, now, exception);
                }

                recurringJob.IsChanged(now, out var changedFields);
                transaction.UpdateRecurringJob(recurringJob, changedFields, _logger);

                // We should commit transaction outside of the internal try/catch block, because these
                // exceptions are always due to network issues, and in case of a timeout exception we
                // can't determine whether it was actually succeeded or not, and can't perform an
                // idempotent retry in this case.
                transaction.Commit();
            }
        }

        private void RetryRecurringJob(string recurringJobId, RecurringJobEntity recurringJob, DateTime now, Exception error)
        {
            var errorString = error.ToStringWithOriginalStackTrace(States.FailedState.MaxLinesInExceptionDetails);

            if (recurringJob.RetryAttempt < MaxRetryAttemptCount)
            {
                var delay = _pollingDelay > TimeSpan.Zero ? _pollingDelay : TimeSpan.FromMinutes(1);

                _logger.WarnException(
                    $"Recurring job '{recurringJobId}' can't be scheduled due to an error and will be retried in {delay}.",
                    error);
                recurringJob.ScheduleRetry(now.Add(delay), errorString);
            }
            else
            {
                _logger.ErrorException(
                    $"Recurring job '{recurringJobId}' can't be scheduled due to an error and will be disabled.", error);
                recurringJob.Disable(errorString);
            }
        }

        private void RemoveRecurringJob(IStorageConnection connection, string recurringJobId)
        {
            _logger.Debug($"Recurring job '{recurringJobId}' doesn't exist and will be removed from schedule.");

            using (var transaction = connection.CreateWriteTransaction())
            {
                transaction.RemoveFromSet("recurring-jobs", recurringJobId);
                transaction.Commit();
            }
        }

        private T UseConnectionDistributedLock<T>(JobStorage storage, Func<IStorageConnection, T> action)
        {
            var resource = "recurring-jobs:lock";
            try
            {
                using (var connection = storage.GetConnection())
                using (connection.AcquireDistributedLock(resource, LockTimeout))
                {
                    return action(connection);
                }
            }
            catch (DistributedLockTimeoutException e) when (e.Resource.EndsWith(resource))
            {
                // DistributedLockTimeoutException here doesn't mean that recurring jobs weren't scheduled.
                // It just means another Hangfire server did this work.
                _logger.Log(
                    LogLevel.Debug,
                    () => $@"An exception was thrown during acquiring distributed lock the {resource} resource within {LockTimeout.TotalSeconds} seconds. The recurring jobs have not been handled this time.",
                    e);
            }

            return default;
        }

        private bool IsBatchingAvailable(IStorageConnection connection)
        {
            return _isBatchingAvailableCache.GetOrAdd(
                connection.GetType(),
                type =>
                {
                    if (connection is JobStorageConnection storageConnection)
                    {
                        try
                        {
                            storageConnection.GetFirstByLowestScoreFromSet(null, 0, 0, 1);
                        }
                        catch (ArgumentNullException ex) when (ex.ParamName == "key")
                        {
                            return true;
                        }
                        catch (Exception ex) when (ex.IsCatchableExceptionType())
                        {
                            //
                        }
                    }

                    return false;
                });
        }
    }
}