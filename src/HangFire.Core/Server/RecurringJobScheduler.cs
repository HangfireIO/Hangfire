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
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using NCrontab;

namespace HangFire.Server
{
    public class RecurringJobScheduler : IServerComponent
    {
        private readonly JobStorage _storage;
        private readonly IBackgroundJobClient _client;

        public RecurringJobScheduler(JobStorage storage, IBackgroundJobClient client)
        {
            _storage = storage;
            _client = client;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            using (connection.AcquireDistributedLock("recurring-jobs:lock", TimeSpan.FromMinutes(1)))
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

                    var serializedJob = JobHelper.FromJson<InvocationData>(recurringJob["Job"]);
                    var job = serializedJob.Deserialize();
                    var cron = recurringJob["Cron"];
                    var cronSchedule = CrontabSchedule.Parse(cron);

                    var nextExecution = recurringJob.ContainsKey("NextExecution")
                        ? JobHelper.FromStringTimestamp(recurringJob["NextExecution"])
                        : cronSchedule.GetNextOccurrence(DateTime.UtcNow);

                    if (nextExecution <= DateTime.UtcNow)
                    {
                        var state = new EnqueuedState { Reason = "Enqueued as recurring job" };
                        var jobId = _client.Create(job, state);

                        connection.SetRangeInHash(
                            String.Format("recurring-job:{0}", recurringJobId),
                            new Dictionary<string, string>
                            {
                                { "LastExecution", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                                { "LastJobId", jobId },
                                { "NextExecution", JobHelper.ToStringTimestamp(cronSchedule.GetNextOccurrence(nextExecution)) }
                            });
                    }
                    else if (!recurringJob.ContainsKey("NextExecution"))
                    {
                        connection.SetRangeInHash(
                            String.Format("recurring-job:{0}", recurringJobId),
                            new Dictionary<string, string>
                            {
                                { "NextExecution", JobHelper.ToStringTimestamp(nextExecution) }
                            });
                    }
                }
            }

            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
        }

        public override string ToString()
        {
            return "Recurring Job Scheduler";
        }
    }
}
