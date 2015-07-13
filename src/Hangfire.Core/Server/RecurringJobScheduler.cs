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
    internal class RecurringJobScheduler : IBackgroundProcess
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly Func<JobStorage, IStateMachineFactory> _stateMachineFactory;
        private readonly IJobCreationProcess _creationProcess;
        private readonly Func<CrontabSchedule, TimeZoneInfo, IScheduleInstant> _instantFactory;
        private readonly IThrottler _throttler;

        public RecurringJobScheduler()
            : this(StateMachineFactory.Default, new DefaultJobCreationProcess(), ScheduleInstant.Factory, new EveryMinuteThrottler())
        {
        }

        public RecurringJobScheduler(
            [NotNull] Func<JobStorage, IStateMachineFactory> stateMachineFactory,
            [NotNull] IJobCreationProcess creationProcess,
            [NotNull] Func<CrontabSchedule, TimeZoneInfo, IScheduleInstant> instantFactory,
            [NotNull] IThrottler throttler)
        {
            if (stateMachineFactory == null) throw new ArgumentNullException("stateMachineFactory");
            if (creationProcess == null) throw new ArgumentNullException("creationProcess");
            if (instantFactory == null) throw new ArgumentNullException("instantFactory");
            if (throttler == null) throw new ArgumentNullException("throttler");

            _stateMachineFactory = stateMachineFactory;
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
                var stateMachineFactory = _stateMachineFactory(context.Storage);

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
                        TryScheduleJob(connection, recurringJobId, recurringJob, stateMachineFactory);
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
            return "Recurring Job Scheduler";
        }

        private void TryScheduleJob(
            IStorageConnection connection, 
            string recurringJobId, 
            Dictionary<string, string> recurringJob,
            IStateMachineFactory stateMachineFactory)
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
                    var jobId = CreateBackgroundJob(connection, job, state, stateMachineFactory);

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

        private string CreateBackgroundJob(IStorageConnection connection, Job job, IState state, IStateMachineFactory stateMachineFactory)
        {
            var context = new CreateContext(connection, job, state);
            var stateMachine = stateMachineFactory.Create(connection);

            return _creationProcess.Run(context, stateMachine);
        }
    }
}
