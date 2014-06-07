// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Threading;
using HangFire.Annotations;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using NCrontab;

namespace HangFire.Server
{
    public class RecurringJobScheduler : IServerComponent
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);

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

                    TryScheduleJob(connection, recurringJobId, recurringJob);
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
                var nextExecution = JobHelper.FromStringTimestamp(recurringJob["NextExecution"]);

                if (nextExecution <= currentTime)
                {
                    var state = new EnqueuedState { Reason = "Triggered by recurring job scheduler" };
                    var jobId = _client.Create(job, state);

                    connection.SetRangeInHash(
                        String.Format("recurring-job:{0}", recurringJobId),
                        new Dictionary<string, string>
                        {
                            { "LastExecution", JobHelper.ToStringTimestamp(currentTime) },
                            { "LastJobId", jobId },
                            { "NextExecution", JobHelper.ToStringTimestamp(_dateTimeProvider.GetNextOccurrence(cronSchedule)) }
                        });
                }
            }
            else
            {
                var nextExecution = _dateTimeProvider.GetNextOccurrence(cronSchedule);

                connection.SetRangeInHash(
                    String.Format("recurring-job:{0}", recurringJobId),
                    new Dictionary<string, string>
                    {
                        { "NextExecution", JobHelper.ToStringTimestamp(nextExecution) }
                    });
            }
        }

        public override string ToString()
        {
            return "Recurring Job Scheduler";
        }
    }
}
