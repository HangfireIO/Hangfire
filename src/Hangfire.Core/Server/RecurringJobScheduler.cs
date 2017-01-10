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
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;
using NCrontab;

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
        private static readonly ILog Logger = LogProvider.For<RecurringJobScheduler>();
        
        private readonly IBackgroundJobFactory _factory;
        private readonly Func<CrontabSchedule, TimeZoneInfo, IScheduleInstant> _instantFactory;
        private readonly IThrottler _throttler;

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
            : this(factory, ScheduleInstant.Factory, new EveryMinuteThrottler())
        {
        }

        internal RecurringJobScheduler(
            [NotNull] IBackgroundJobFactory factory,
            [NotNull] Func<CrontabSchedule, TimeZoneInfo, IScheduleInstant> instantFactory,
            [NotNull] IThrottler throttler)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (instantFactory == null) throw new ArgumentNullException(nameof(instantFactory));
            if (throttler == null) throw new ArgumentNullException(nameof(throttler));
            
            _factory = factory;
            _instantFactory = instantFactory;
            _throttler = throttler;
        }

        /// <inheritdoc />
        public void Execute(BackgroundProcessContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            _throttler.Throttle(context.CancellationToken);

            using (var connection = context.Storage.GetConnection())
            {
                IDisposable distributedLock;

                try
                {
                    distributedLock = connection.AcquireDistributedLock("recurring-jobs:lock", LockTimeout);
                }
                catch (DistributedLockTimeoutException)
                {
                    // DistributedLockTimeoutException here doesn't mean that recurring jobs weren't scheduled.
                    // It just means another Hangfire server did this work.

                    _throttler.Delay(context.CancellationToken);
                    return;
                }

                using (distributedLock)
                {
                    var recurringJobIds = connection.GetAllItemsFromSet("recurring-jobs");

                    foreach (var recurringJobId in recurringJobIds)
                    {
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
                            Logger.WarnException(
                                $"Recurring job '{recurringJobId}' can not be scheduled due to job load exception.",
                                ex);
                        }
                    }

                    _throttler.Delay(context.CancellationToken);
                }
            }
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
            var serializedJob = JobHelper.FromJson<InvocationData>(recurringJob["Job"]);
            var job = serializedJob.Deserialize();
            var cron = recurringJob["Cron"];
            var cronSchedule = CrontabSchedule.Parse(cron);

            try
            {
                var timeZone = recurringJob.ContainsKey("TimeZoneId")
                    ? TimeZoneInfo.FindSystemTimeZoneById(recurringJob["TimeZoneId"])
                    : TimeZoneInfo.Utc;

                var nowInstant = _instantFactory(cronSchedule, timeZone);
                var changedFields = new Dictionary<string, string>();

                var lastInstant = GetLastInstant(recurringJob, nowInstant);
                
                if (nowInstant.GetNextInstants(lastInstant).Any())
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
                        Logger.Debug($"Recurring job '{recurringJobId}' execution at '{nowInstant.NowInstant}' has been canceled.");
                    }

                    changedFields.Add("LastExecution", JobHelper.SerializeDateTime(nowInstant.NowInstant));
                    changedFields.Add("LastJobId", jobId ?? String.Empty);
                }
                
                // Fixing old recurring jobs that doesn't have the CreatedAt field
                if (!recurringJob.ContainsKey("CreatedAt"))
                {
                    changedFields.Add("CreatedAt", JobHelper.SerializeDateTime(nowInstant.NowInstant));
                }
                    
                changedFields.Add("NextExecution", nowInstant.NextInstant.HasValue ? JobHelper.SerializeDateTime(nowInstant.NextInstant.Value) : null);

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

                Logger.ErrorException(
                    $"Recurring job '{recurringJobId}' was not triggered: {ex.Message}.",
                    ex);
            }

        }

        private static DateTime GetLastInstant(IReadOnlyDictionary<string, string> recurringJob, IScheduleInstant instant)
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
                lastInstant = instant.NowInstant.AddSeconds(-1);
            }

            return lastInstant;
        }
    }
}