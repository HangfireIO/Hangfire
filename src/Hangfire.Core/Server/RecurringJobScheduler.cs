// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
        private static readonly int MaxRetryAttemptCount = 10;

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

            var jobsEnqueued = 0;

            while (EnqueueNextRecurringJobs(context))
            {
                jobsEnqueued++;

                if (context.IsStopping)
                {
                    break;
                }
            }

            if (jobsEnqueued != 0)
            {
                _logger.Debug($"{jobsEnqueued} recurring job(s) enqueued.");
            }

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

        private bool EnqueueNextRecurringJobs(BackgroundProcessContext context)
        {
            return UseConnectionDistributedLock(context.Storage, connection =>
            {
                var result = false;

                if (IsBatchingAvailable(connection))
                {
                    var now = _nowFactory();
                    var timestamp = JobHelper.ToTimestamp(now);
                    var recurringJobIds = ((JobStorageConnection)connection).GetFirstByLowestScoreFromSet("recurring-jobs", 0, timestamp, BatchSize);

                    if (recurringJobIds == null || recurringJobIds.Count == 0) return false;

                    foreach (var recurringJobId in recurringJobIds)
                    {
                        if (context.IsStopping) return false;

                        if (TryEnqueueBackgroundJob(context, connection, recurringJobId, now))
                        {
                            result = true;
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < BatchSize; i++)
                    {
                        if (context.IsStopping) return false;

                        var now = _nowFactory();
                        var timestamp = JobHelper.ToTimestamp(now);

                        var recurringJobId = connection.GetFirstByLowestScoreFromSet("recurring-jobs", 0, timestamp);
                        if (recurringJobId == null) return false;

                        if (!TryEnqueueBackgroundJob(context, connection, recurringJobId, now))
                        {
                            return false;
                        }
                    }
                }

                return result;
            });
        }

        private bool TryEnqueueBackgroundJob(
            BackgroundProcessContext context,
            IStorageConnection connection,
            string recurringJobId,
            DateTime now)
        {
            try
            {
                return EnqueueBackgroundJob(context, connection, recurringJobId, now);
            }
            catch (Exception ex)
            {
                _logger.WarnException(
                    $"Recurring job '{recurringJobId}' can not be scheduled due to an exception.",
                    ex);
            }

            return false;
        }

        private bool EnqueueBackgroundJob(
            BackgroundProcessContext context,
            IStorageConnection connection, 
            string recurringJobId,
            DateTime now)
        {
            using (connection.AcquireDistributedRecurringJobLock(recurringJobId, LockTimeout))
            {
                try
                {
                    var recurringJob = connection.GetRecurringJob(recurringJobId, _timeZoneResolver, now);

                    if (recurringJob == null)
                    {
                        using (var transaction = connection.CreateWriteTransaction())
                        {
                            transaction.RemoveFromSet("recurring-jobs", recurringJobId);
                            transaction.Commit();
                        }

                        return false;
                    }

                    // If a recurring job has the "V" field, then it was created by a newer
                    // version. Despite we can handle 1.7.0-based recurring jobs just fine,
                    // future versions may introduce new features anyway, so it's safer to
                    // let other servers to handle this recurring job.
                    if (recurringJob.Version.HasValue && recurringJob.Version > 2)
                    {
                        return false;
                    }

                    BackgroundJob backgroundJob = null;
                    IReadOnlyDictionary<string, string> changedFields;

                    if (recurringJob.TrySchedule(out var nextExecution, out var error))
                    {
                        if (nextExecution.HasValue && nextExecution <= now)
                        {
                            backgroundJob = _factory.TriggerRecurringJob(context.Storage, connection, _profiler, recurringJob, now);

                            if (String.IsNullOrEmpty(backgroundJob?.Id))
                            {
                                _logger.Debug($"Recurring job '{recurringJobId}' execution at '{nextExecution}' has been canceled.");
                            }
                        }

                        recurringJob.IsChanged(out changedFields, out nextExecution);
                    }
                    else if (recurringJob.RetryAttempt < MaxRetryAttemptCount)
                    {
                        var delay = _pollingDelay > TimeSpan.Zero ? _pollingDelay : TimeSpan.FromMinutes(1);
                        
                        _logger.WarnException($"Recurring job '{recurringJobId}' can't be scheduled due to an error and will be retried in {delay}.", error);
                        recurringJob.ScheduleRetry(delay, out changedFields, out nextExecution);
                    }
                    else
                    {
                        _logger.ErrorException($"Recurring job '{recurringJobId}' can't be scheduled due to an error and will be disabled.", error);
                        recurringJob.Disable(error, out changedFields, out nextExecution);
                    }

                    // We always start a transaction, regardless our recurring job was updated or not,
                    // to prevent from infinite loop, when there's an old processing server (pre-1.7.0)
                    // in our environment that doesn't know it should modify the score for entries in
                    // the recurring jobs set.
                    using (var transaction = connection.CreateWriteTransaction())
                    {
                        if (backgroundJob != null)
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

                        transaction.UpdateRecurringJob(recurringJob, changedFields, nextExecution, _logger);

                        transaction.Commit();
                        return true;
                    }
                }
#if !NETSTANDARD1_3
                catch (TimeZoneNotFoundException ex)
                {
#else
                catch (Exception ex)
                {
                    // https://github.com/dotnet/corefx/issues/7552
                    if (!ex.GetType().Name.Equals("TimeZoneNotFoundException")) throw;
#endif

                    _logger.ErrorException(
                        $"Recurring job '{recurringJobId}' was not triggered: {ex.Message}.",
                        ex);
                }

                return false;
            }
        }

        private bool UseConnectionDistributedLock(JobStorage storage, Func<IStorageConnection, bool> action)
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

            return false;
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
                        catch (Exception)
                        {
                            //
                        }
                    }

                    return false;
                });
        }
    }
}