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
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;
using Cronos;

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

        private readonly ILog _logger = LogProvider.For<RecurringJobScheduler>();

        private readonly IBackgroundJobFactory _factory;
        private readonly IStateMachine _stateMachine;
        private readonly Func<DateTime> _nowFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobScheduler"/>
        /// class with default background job factory.
        /// </summary>
        public RecurringJobScheduler()
            : this(new BackgroundJobFactory(GlobalJobFilters.Filters), new StateMachine(GlobalJobFilters.Filters))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobScheduler"/>
        /// class with custom background job factory and a state machine.
        /// </summary>
        /// <param name="factory">Factory that will be used to create background jobs.</param>
        /// <param name="stateMachine">State machine that's responsible for enqueuing jobs.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stateMachine"/> is null.</exception>
        public RecurringJobScheduler(
            [NotNull] IBackgroundJobFactory factory,
            [NotNull] IStateMachine stateMachine)
            : this(factory, stateMachine, () => DateTime.UtcNow)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobScheduler"/> class
        /// with custom background job factory, state machine and clocks.
        /// </summary>
        /// <param name="factory">Factory that will be used to create background jobs.</param>
        /// <param name="stateMachine">State machine that's responsible for enqueuing jobs.</param>
        /// <param name="nowFactory">Factory function that returns the current time.</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stateMachine"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="nowFactory"/> is null.</exception>
        public RecurringJobScheduler(
            [NotNull] IBackgroundJobFactory factory,
            [NotNull] IStateMachine stateMachine,
            [NotNull] Func<DateTime> nowFactory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (stateMachine == null) throw new ArgumentNullException(nameof(stateMachine));
            if (nowFactory == null) throw new ArgumentNullException(nameof(nowFactory));

            _factory = factory;
            _stateMachine = stateMachine;
            _nowFactory = nowFactory;
        }

        /// <inheritdoc />
        public void Execute(BackgroundProcessContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var jobsEnqueued = 0;

            while (EnqueueNextRecurringJobs(context))
            {
                jobsEnqueued++;

                if (context.IsShutdownRequested)
                {
                    break;
                }
            }

            if (jobsEnqueued != 0)
            {
                _logger.Info($"{jobsEnqueued} recurring job(s) enqueued.");
            }

            var now = DateTime.UtcNow;
            context.Wait(now.AddMilliseconds(-now.Millisecond).AddSeconds(-now.Second).AddMinutes(1) - now);
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
                if (IsBatchingAvailable(connection))
                {
                    var now = _nowFactory();
                    var timestamp = JobHelper.ToTimestamp(now);
                    var recurringJobIds = ((JobStorageConnection)connection).GetFirstByLowestScoreFromSet("recurring-jobs", 0, timestamp, BatchSize);

                    if (recurringJobIds == null || recurringJobIds.Count == 0) return false;

                    foreach (var recurringJobId in recurringJobIds)
                    {
                        if (context.IsShutdownRequested) return false;
                        EnqueueBackgroundJob(context, connection, recurringJobId, now);
                    }
                }
                else
                {
                    for (var i = 0; i < BatchSize; i++)
                    {
                        if (context.IsShutdownRequested) return false;

                        var now = _nowFactory();
                        var timestamp = JobHelper.ToTimestamp(now);

                        var recurringJobId = connection.GetFirstByLowestScoreFromSet("recurring-jobs", 0, timestamp);
                        if (recurringJobId == null) return false;

                        EnqueueBackgroundJob(context, connection, recurringJobId, now);
                    }
                }

                return true;
            });
        }

        private void EnqueueBackgroundJob(
            BackgroundProcessContext context,
            IStorageConnection connection, 
            string recurringJobId,
            DateTime now)
        {
            using (connection.AcquireDistributedRecurringJobLock(recurringJobId, LockTimeout))
            {
                var recurringJob = connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");

                if (recurringJob == null || recurringJob.Count == 0)
                {
                    using (var transaction = connection.CreateWriteTransaction())
                    {
                        transaction.RemoveFromSet("recurring-jobs", recurringJobId);
                        transaction.Commit();
                    }

                    return;
                }

                try
                {
                    var nextExecution = recurringJob.ContainsKey("NextExecution")
                        ? JobHelper.DeserializeNullableDateTime(recurringJob["NextExecution"])
                        : null;

                    var changedFields = new Dictionary<string, string>();
                    var timeZone = recurringJob.ContainsKey("TimeZoneId")
                        ? TimeZoneInfo.FindSystemTimeZoneById(recurringJob["TimeZoneId"])
                        : TimeZoneInfo.Utc;

                    BackgroundJob backgroundJob = null;
                    EnqueuedState state = null;

                    if (nextExecution.HasValue && nextExecution <= now)
                    {
                        state = new EnqueuedState { Reason = "Triggered by recurring job scheduler" };
                        if (recurringJob.ContainsKey("Queue") && !String.IsNullOrEmpty(recurringJob["Queue"]))
                        {
                            state.Queue = recurringJob["Queue"];
                        }

                        var job = InvocationData.Deserialize(recurringJob["Job"]).Deserialize();
                        var createContext = new CreateContext(context.Storage, connection, job, null);
                        createContext.Parameters["RecurringJobId"] = recurringJobId;

                        backgroundJob = _factory.Create(createContext);
                        var jobId = backgroundJob?.Id;

                        if (String.IsNullOrEmpty(jobId))
                        {
                            _logger.Debug($"Recurring job '{recurringJobId}' execution at '{nextExecution}' has been canceled.");
                        }

                        changedFields.Add("LastExecution", JobHelper.SerializeDateTime(now));
                        changedFields.Add("LastJobId", jobId ?? String.Empty);

                        // Fixing old recurring jobs that doesn't have the CreatedAt field
                        if (!recurringJob.ContainsKey("CreatedAt"))
                        {
                            changedFields.Add("CreatedAt", JobHelper.SerializeDateTime(now));
                        }
                    }

                    nextExecution = CronExpression.Parse(recurringJob["Cron"]).GetNextOccurrence(
                        now,
                        timeZone,
                        inclusive: false);

                    changedFields.Add("NextExecution", nextExecution.HasValue ? JobHelper.SerializeDateTime(nextExecution.Value) : String.Empty);

                    if (backgroundJob != null || changedFields.Count != 0)
                    {
                        using (var transaction = connection.CreateWriteTransaction())
                        {
                            if (backgroundJob != null)
                            {
                                _stateMachine.ApplyState(new ApplyStateContext(
                                    context.Storage,
                                    connection,
                                    transaction,
                                    backgroundJob,
                                    state,
                                    null));
                            }

                            transaction.SetRangeInHash($"recurring-job:{recurringJobId}", changedFields);
                            transaction.AddToSet(
                                "recurring-jobs",
                                recurringJobId,
                                nextExecution.HasValue ? JobHelper.ToTimestamp(nextExecution.Value) : double.MaxValue);

                            transaction.Commit();
                        }
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
            catch (DistributedLockTimeoutException e) when (e.Resource == resource)
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

        private static bool IsBatchingAvailable(IStorageConnection connection)
        {
            var batchingAvailable = false;
            if (connection is JobStorageConnection storageConnection)
            {
                try
                {
                    storageConnection.GetFirstByLowestScoreFromSet(null, 0, 0, 1);
                }
                catch (ArgumentNullException)
                {
                    batchingAvailable = true;
                }
            }

            return batchingAvailable;
        }
    }
}