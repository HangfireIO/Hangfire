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
using HangFire.Annotations;
using HangFire.Common;
using HangFire.Storage;
using NCrontab;

namespace HangFire
{
    /// <summary>
    /// Represents a recurring job manager that allows to create, update
    /// or delete recurring jobs.
    /// </summary>
    public class RecurringJobManager
    {
        private readonly JobStorage _storage;

        public RecurringJobManager([NotNull] JobStorage storage)
        {
            if (storage == null) throw new ArgumentNullException("storage");

            _storage = storage;
        }

        public void AddOrUpdate(
            [NotNull] string recurringJobId, 
            [NotNull] Job job, 
            [NotNull] string cronExpression)
        {
            if (recurringJobId == null) throw new ArgumentNullException("recurringJobId");
            if (job == null) throw new ArgumentNullException("job");
            if (cronExpression == null) throw new ArgumentNullException("cronExpression");

            CrontabSchedule.Parse(cronExpression);

            using (var connection = _storage.GetConnection())
            {
                var recurringJob = new Dictionary<string, string>();
                var invocationData = InvocationData.Serialize(job);
                
                recurringJob["Job"] = JobHelper.ToJson(invocationData);
                recurringJob["Cron"] = cronExpression;

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
    }
}
