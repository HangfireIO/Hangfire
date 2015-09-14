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
    public class RecurringJobScheduler : IBackgroundProcess
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        
        private readonly IJobCreationProcess _creationProcess;
        private readonly Func<CrontabSchedule, TimeZoneInfo, IScheduleInstant> _instantFactory;
        private readonly IThrottler _throttler;

        public RecurringJobScheduler()
            : this(new JobCreationProcess())
        {
        }
        
        public RecurringJobScheduler(
            [NotNull] IJobCreationProcess creationProcess)
            : this(creationProcess, ScheduleInstant.Factory, new EveryMinuteThrottler())
        {
        }

        internal RecurringJobScheduler(
            [NotNull] IJobCreationProcess creationProcess,
            [NotNull] Func<CrontabSchedule, TimeZoneInfo, IScheduleInstant> instantFactory,
            [NotNull] IThrottler throttler)
        {
            if (creationProcess == null) throw new ArgumentNullException("creationProcess");
            if (instantFactory == null) throw new ArgumentNullException("instantFactory");
            if (throttler == null) throw new ArgumentNullException("throttler");
            
            _creationProcess = creationProcess;
            _instantFactory = instantFactory;
            _throttler = throttler;
        }

        public void Execute(BackgroundProcessContext context)
        {
            _throttler.Throttle(context.CancellationToken);

            using (var connection = context.Storage.GetConnection())
            using (connection.AcquireDistributedLock("recurring-jobs:lock", LockTimeout))
            {
                var recurringJobIds = connection.GetAllItemsFromSet("recurring-jobs");

                foreach (var recurringJobId in recurringJobIds)
                {
                    var recurringJob = connection.GetAllEntriesFromHash(
                        String.Format("recurring-job:{0}", recurringJobId));

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
                            String.Format(
                                "Recurring job '{0}' can not be scheduled due to job load exception.",
                                recurringJobId),
                            ex);
                    }
                }

                _throttler.Delay(context.CancellationToken);
            }
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        private void TryScheduleJob(
            JobStorage storage,
            IStorageConnection connection, 
            string recurringJobId, 
            Dictionary<string, string> recurringJob)
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

                var instant = _instantFactory(cronSchedule, timeZone);

                var lastExecutionTime = recurringJob.ContainsKey("LastExecution")
                    ? JobHelper.DeserializeDateTime(recurringJob["LastExecution"])
                    : (DateTime?)null;

                var changedFields = new Dictionary<string, string>();

                if (instant.GetNextInstants(lastExecutionTime).Any())
                {
                    var state = new EnqueuedState { Reason = "Triggered by recurring job scheduler" };
                    if (recurringJob.ContainsKey("Queue") && !String.IsNullOrEmpty(recurringJob["Queue"]))
                    {
                        state.Queue = recurringJob["Queue"];
                    }

                    var context = new CreateContext(storage, connection, job, state);
                    var jobId = _creationProcess.Run(context);

                    if (String.IsNullOrEmpty(jobId))
                    {
                        Logger.DebugFormat(
                            "Recurring job '{0}' execution at '{1}' has been canceled.",
                            recurringJobId,
                            instant.NowInstant);
                    }

                    changedFields.Add("LastExecution", JobHelper.SerializeDateTime(instant.NowInstant));
                    changedFields.Add("LastJobId", jobId ?? String.Empty);
                }

                changedFields.Add("NextExecution", JobHelper.SerializeDateTime(instant.NextInstant));

                connection.SetRangeInHash(
                    String.Format("recurring-job:{0}", recurringJobId),
                    changedFields);
            }
            catch (TimeZoneNotFoundException ex)
            {
                Logger.ErrorException(
                    String.Format("Recurring job '{0}' was not triggered: {1}.", recurringJobId, ex.Message),
                    ex);
            }
        }
    }
}
