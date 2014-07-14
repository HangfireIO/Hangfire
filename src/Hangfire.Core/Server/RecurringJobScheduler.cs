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
using System.Threading;
using Common.Logging;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using NCrontab;

namespace Hangfire.Server
{
    public class RecurringJobScheduler : IServerComponent
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly JobStorage _storage;
        private readonly IBackgroundJobClient _client;
        private readonly IDateTimeProvider _dateTimeProvider;

        public RecurringJobScheduler(
            [NotNull] JobStorage storage, 
            [NotNull] IBackgroundJobClient client, 
            [NotNull] IDateTimeProvider dateTimeProvider)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (client == null) throw new ArgumentNullException("client");
            if (dateTimeProvider == null) throw new ArgumentNullException("dateTimeProvider");

            _storage = storage;
            _client = client;
            _dateTimeProvider = dateTimeProvider;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            while (_dateTimeProvider.CurrentDateTime.Second != 0)
            {
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                cancellationToken.ThrowIfCancellationRequested();
            }

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
                        Logger.WarnFormat("Recurring job '{0}' can not be scheduled due to job load exception.", ex, recurringJobId);
                    }
                    
                }
            }
        }

        private void TryScheduleJob(IStorageConnection connection, string recurringJobId, Dictionary<string, string> recurringJob)
        {
            var serializedJob = JobHelper.FromJson<InvocationData>(recurringJob["Job"]);
            var job = serializedJob.Deserialize();
            var cron = recurringJob["Cron"];
            var cronSchedule = CrontabSchedule.Parse(cron);

            var currentTime = _dateTimeProvider.CurrentDateTime;

            if (recurringJob.ContainsKey("NextExecution"))
            {
                var nextExecution = JobHelper.DeserializeDateTime(recurringJob["NextExecution"]);

                if (nextExecution <= currentTime)
                {
                    var state = new EnqueuedState { Reason = "Triggered by recurring job scheduler" };
                    var jobId = _client.Create(job, state);

                    connection.SetRangeInHash(
                        String.Format("recurring-job:{0}", recurringJobId),
                        new Dictionary<string, string>
                        {
                            { "LastExecution", JobHelper.SerializeDateTime(currentTime) },
                            { "LastJobId", jobId },
                            { "NextExecution", JobHelper.SerializeDateTime(_dateTimeProvider.GetNextOccurrence(cronSchedule)) }
                        });
                }
                else
                {
                    TryUpdateNextExecutionTime(connection, recurringJobId, cronSchedule);
                }
            }
            else
            {
                TryUpdateNextExecutionTime(connection, recurringJobId, cronSchedule);
            }
        }

        private void TryUpdateNextExecutionTime(IStorageConnection connection, string recurringJobId, CrontabSchedule cronSchedule)
        {
            connection.SetRangeInHash(
                String.Format("recurring-job:{0}", recurringJobId),
                new Dictionary<string, string>
                    {
                        { "NextExecution", JobHelper.SerializeDateTime(_dateTimeProvider.GetNextOccurrence(cronSchedule)) }
                    });
        }
        
        public override string ToString()
        {
            return "Recurring Job Scheduler";
        }
    }
}
