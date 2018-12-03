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

        private static readonly Func<DateTime> DefaultNowInstantFactory = () =>
        {
            var now = DateTime.UtcNow;
            return now.AddSeconds(-now.Second);
        };

        private readonly ILog _logger = LogProvider.For<RecurringJobScheduler>();
        private readonly IBackgroundJobFactory _factory;
        private readonly IThrottler _throttler;
        private readonly Func<DateTime> _nowInstantFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobScheduler"/>
        /// class with default background job factory.
        /// </summary>
        public RecurringJobScheduler()
            : this(new BackgroundJobFactory())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobScheduler"/>
        /// class with custom background job factory.
        /// </summary>
        /// <param name="factory">Factory that will be used to create background jobs.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        public RecurringJobScheduler([NotNull] IBackgroundJobFactory factory)
            : this(factory, DefaultNowInstantFactory, new EveryMinuteThrottler())
        {
        }

        internal RecurringJobScheduler(
            [NotNull] IBackgroundJobFactory factory,
            [NotNull] Func<DateTime> nowInstantFactory,
            [NotNull] IThrottler throttler)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (nowInstantFactory == null) throw new ArgumentNullException(nameof(nowInstantFactory));
            if (throttler == null) throw new ArgumentNullException(nameof(throttler));
            
            _factory = factory;
            _nowInstantFactory = nowInstantFactory;
            _throttler = throttler;
        }

        /// <inheritdoc />
        public void Execute(BackgroundProcessContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            _throttler.Throttle(context.CancellationToken);

            UseConnectionDistributedLock(context.Storage, connection =>
            {
                var recurringJobIds = connection.GetAllItemsFromSet("recurring-jobs");

                foreach (var recurringJobId in recurringJobIds)
                {
                    if (context.IsShutdownRequested)
                    {
                        return;
                    }

                    var recurringJob = connection.GetAllEntriesFromHash(
                        $"recurring-job:{recurringJobId}");

                    if (recurringJob == null)
                    {
                        continue;
                    }

                    try
                    {
                        TryScheduleJob(context.Storage, connection, recurringJobId, recurringJob);
                    }
                    catch (JobLoadException ex)
                    {
                        _logger.WarnException(
                            $"Recurring job '{recurringJobId}' can not be scheduled due to job load exception.",
                            ex);
                    }
                }
            });

            // The code above may be completed in less than a second. Default throttler use
            // the second resolution, and without an extra delay, CPU and DB bursts may happen.
            _throttler.Delay(context.CancellationToken);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GetType().Name;
        }

        private void TryScheduleJob(
            JobStorage storage,
            IStorageConnection connection, 
            string recurringJobId, 
            IReadOnlyDictionary<string, string> recurringJob)
        {
            var serializedJob = InvocationData.Deserialize(recurringJob["Job"]);
            var job = serializedJob.Deserialize();
            var cron = recurringJob["Cron"];
            var cronExpression = CronExpression.Parse(cron);

            try
            {
                var timeZone = recurringJob.ContainsKey("TimeZoneId")
                    ? TimeZoneInfo.FindSystemTimeZoneById(recurringJob["TimeZoneId"])
                    : TimeZoneInfo.Utc;

                var nowInstant = _nowInstantFactory();
                var lastInstant = GetLastInstant(recurringJob, nowInstant);

                var changedFields = new Dictionary<string, string>();

                var nextInstant = cronExpression.GetNextOccurrence(lastInstant, timeZone);

                if (nextInstant <= nowInstant)
                {
                    var state = new EnqueuedState { Reason = "Triggered by recurring job scheduler" };
                    if (recurringJob.ContainsKey("Queue") && !String.IsNullOrEmpty(recurringJob["Queue"]))
                    {
                        state.Queue = recurringJob["Queue"];
                    }

                    var context = new CreateContext(storage, connection, job, state);
                    context.Parameters["RecurringJobId"] = recurringJobId;

                    var backgroundJob = _factory.Create(context);
                    var jobId = backgroundJob?.Id;

                    if (String.IsNullOrEmpty(jobId))
                    {
                        _logger.Debug($"Recurring job '{recurringJobId}' execution at '{nowInstant}' has been canceled.");
                    }

                    changedFields.Add("LastExecution", JobHelper.SerializeDateTime(nowInstant));
                    changedFields.Add("LastJobId", jobId ?? String.Empty);

                    nextInstant = cronExpression.GetNextOccurrence(nowInstant, timeZone);
                }
                
                // Fixing old recurring jobs that doesn't have the CreatedAt field
                if (!recurringJob.ContainsKey("CreatedAt"))
                {
                    changedFields.Add("CreatedAt", JobHelper.SerializeDateTime(nowInstant));
                }
                    
                changedFields.Add("NextExecution", nextInstant.HasValue ? JobHelper.SerializeDateTime(nextInstant.Value) : null);

                connection.SetRangeInHash(
                    $"recurring-job:{recurringJobId}",
                    changedFields);
            }
#if NETFULL
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

        private void UseConnectionDistributedLock(JobStorage storage, Action<IStorageConnection> action)
        {
            var resource = "recurring-jobs:lock";
            try
            {
                using (var connection = storage.GetConnection())
                using (connection.AcquireDistributedLock(resource, LockTimeout))
                {
                    action(connection);
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
        }

        private static DateTime GetLastInstant(IReadOnlyDictionary<string, string> recurringJob, DateTime nowInstant)
        {
            DateTime lastInstant;

            if (recurringJob.ContainsKey("LastExecution"))
            {
                lastInstant = JobHelper.DeserializeDateTime(recurringJob["LastExecution"]);
            }
            else if (recurringJob.ContainsKey("CreatedAt"))
            {
                lastInstant = JobHelper.DeserializeDateTime(recurringJob["CreatedAt"]);
            }
            else if (recurringJob.ContainsKey("NextExecution"))
            {
                lastInstant = JobHelper.DeserializeDateTime(recurringJob["NextExecution"]);
                lastInstant = lastInstant.AddSeconds(-1);
            }
            else
            {
                lastInstant = nowInstant.AddSeconds(-1);
            }

            return lastInstant;
        }
    }
}