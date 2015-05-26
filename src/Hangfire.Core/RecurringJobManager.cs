﻿// This file is part of Hangfire.
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
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using NCrontab;

namespace Hangfire
{
    /// <summary>
    /// Represents a recurring job manager that allows to create, update
    /// or delete recurring jobs.
    /// </summary>
    public class RecurringJobManager
    {
        private readonly JobStorage _storage;
        private readonly IBackgroundJobClient _client;

        public RecurringJobManager()
            : this(JobStorage.Current)
        {
        }

        public RecurringJobManager([NotNull] JobStorage storage)
            : this (storage, new BackgroundJobClient(storage))
        {
        }

        public RecurringJobManager([NotNull] JobStorage storage, [NotNull] IBackgroundJobClient client)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (client == null) throw new ArgumentNullException("client");

            _storage = storage;
            _client = client;
        }

        public void AddOrUpdate(
            [NotNull] string recurringJobId,
            [NotNull] Job job,
            [NotNull] string cronExpression)
        {
            AddOrUpdate(recurringJobId, job, cronExpression, TimeZoneInfo.Utc);
        }

        public void AddOrUpdate(
            [NotNull] string recurringJobId, 
            [NotNull] Job job, 
            [NotNull] string cronExpression, 
            [NotNull] TimeZoneInfo timeZone)
        {
            if (recurringJobId == null) throw new ArgumentNullException("recurringJobId");
            if (job == null) throw new ArgumentNullException("job");
            if (cronExpression == null) throw new ArgumentNullException("cronExpression");
            if (timeZone == null) throw new ArgumentNullException("timeZone");

            ValidateCronExpression(cronExpression);

            using (var connection = _storage.GetConnection())
            {
                var recurringJob = new Dictionary<string, string>();
                var invocationData = InvocationData.Serialize(job);
                
                recurringJob["Job"] = JobHelper.ToJson(invocationData);
                recurringJob["Cron"] = cronExpression;
                recurringJob["TimeZoneId"] = timeZone.Id;

                using (var transaction = connection.CreateWriteTransaction())
                {
                    transaction.SetRangeInHash(
                        String.Format("recurring-job:{0}", recurringJobId), 
                        recurringJob);

                    transaction.AddToSet("recurring-jobs", recurringJobId);

                    transaction.Commit();
                }
            }
        }

        public void Trigger([NotNull] string recurringJobId)
        {
            if (recurringJobId == null) throw new ArgumentNullException("recurringJobId");

            using (var connection = _storage.GetConnection())
            {
                var hash = connection.GetAllEntriesFromHash(String.Format("recurring-job:{0}", recurringJobId));
                if (hash == null)
                {
                    return;
                }
                
                var job = JobHelper.FromJson<InvocationData>(hash["Job"]).Deserialize();
                var state = new EnqueuedState { Reason = "Triggered" };

                _client.Create(job, state);
            }
        }

        public void RemoveIfExists([NotNull] string recurringJobId)
        {
            if (recurringJobId == null) throw new ArgumentNullException("recurringJobId");

            using (var connection = _storage.GetConnection())
            using (var transaction = connection.CreateWriteTransaction())
            {
                transaction.RemoveHash(String.Format("recurring-job:{0}", recurringJobId));
                transaction.RemoveFromSet("recurring-jobs", recurringJobId);

                transaction.Commit();
            }
        }

        private static void ValidateCronExpression(string cronExpression)
        {
            try
            {
                var schedule = CrontabSchedule.Parse(cronExpression);
                schedule.GetNextOccurrence(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("CRON expression is invalid. Please see the inner exception for details.", "cronExpression", ex);
            }
        }
    }
}
