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
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;
using NCrontab;

namespace Hangfire.Server
{
    internal class RecurringJobScheduler : IServerComponent
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly JobStorage _storage;
        private readonly IBackgroundJobClient _client;
        private readonly IScheduleInstantFactory _instantFactory;
        private readonly IThrottler _throttler;

        public RecurringJobScheduler(
            [NotNull] JobStorage storage,
            [NotNull] IBackgroundJobClient client,
            [NotNull] IScheduleInstantFactory instantFactory,
            [NotNull] IThrottler throttler)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (client == null) throw new ArgumentNullException("client");
            if (instantFactory == null) throw new ArgumentNullException("instantFactory");
            if (throttler == null) throw new ArgumentNullException("throttler");

            _storage = storage;
            _client = client;
            _instantFactory = instantFactory;
            _throttler = throttler;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            _throttler.Throttle(cancellationToken);

            using (var connection = _storage.GetConnection())
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
                        TryScheduleJob(connection, recurringJobId, recurringJob);
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

                _throttler.Delay(cancellationToken);
            }
        }

        public override string ToString()
        {
            return "Recurring Job Scheduler";
        }

        private void TryScheduleJob(IStorageConnection connection, string recurringJobId, Dictionary<string, string> recurringJob)
        {
            var serializedJob = JobHelper.FromJson<InvocationData>(recurringJob["Job"]);
            var job = serializedJob.Deserialize();
            var cron = recurringJob["Cron"];
            var cronSchedule = CrontabSchedule.Parse(cron);
            var instant = _instantFactory.GetInstant(cronSchedule);

            var lastExecutionTime = recurringJob.ContainsKey("LastExecution")
                ? JobHelper.DeserializeDateTime(recurringJob["LastExecution"])
                : (DateTime?)null;

            if (instant.GetMatches(lastExecutionTime).Any())
            {
                var state = new EnqueuedState { Reason = "Triggered by recurring job scheduler" };
                var jobId = _client.Create(job, state);

                connection.SetRangeInHash(
                    String.Format("recurring-job:{0}", recurringJobId),
                    new Dictionary<string, string>
                        {
                            { "LastExecution", JobHelper.SerializeDateTime(instant.UtcTime) },
                            { "LastJobId", jobId },
                        });
            }

            connection.SetRangeInHash(
                String.Format("recurring-job:{0}", recurringJobId),
                new Dictionary<string, string>
                {
                    {
                        "NextExecution", 
                        JobHelper.SerializeDateTime(instant.NextOccurrence)
                    }
                });
        }
    }
}
