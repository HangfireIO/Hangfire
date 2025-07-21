// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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

// ReSharper disable RedundantNullnessAttributeWithNullableReferenceTypes
#nullable enable

namespace Hangfire.Storage
{
    public static class StorageConnectionExtensions
    {
        public static IDisposable AcquireDistributedJobLock(
            [NotNull] this IStorageConnection connection, 
            [NotNull] string jobId, 
            TimeSpan timeout)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));

            return connection.AcquireDistributedLock(
                $"job:{jobId}:state-lock",
                timeout);
        }

        public static void AcquireDistributedJobLock(
            [NotNull] this JobStorageTransaction transaction, 
            [NotNull] string jobId, 
            TimeSpan timeout)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));

            transaction.AcquireDistributedLock($"job:{jobId}:state-lock", timeout);
        }

        public static long GetRecurringJobCount([NotNull] this JobStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            return connection.GetSetCount("recurring-jobs");
        }

        public static List<string> GetRecurringJobIds(
            [NotNull] this JobStorageConnection connection,
            int startingFrom,
            int endingAt)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            return connection.GetRangeFromSet("recurring-jobs", startingFrom, endingAt);
        }

        public static List<RecurringJobDto> GetRecurringJobs(
            [NotNull] this JobStorageConnection connection,
            int startingFrom,
            int endingAt)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var ids = connection.GetRecurringJobIds(startingFrom, endingAt);
            return GetRecurringJobDtos(connection, ids);
        }

        public static List<RecurringJobDto> GetRecurringJobs([NotNull] this IStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var ids = connection.GetAllItemsFromSet("recurring-jobs");
            return GetRecurringJobDtos(connection, ids);
        }

        public static List<RecurringJobDto> GetRecurringJobs([NotNull] this IStorageConnection connection, [NotNull] IEnumerable<string> ids)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            return GetRecurringJobDtos(connection, ids);
        }

        private static List<RecurringJobDto> GetRecurringJobDtos(IStorageConnection connection, IEnumerable<string> ids)
        {
            var result = new List<RecurringJobDto>();
            foreach (var id in ids)
            {
                var hash = connection.GetAllEntriesFromHash($"recurring-job:{id}");

                // TODO: Remove this in 2.0 (breaking change)
                if (hash == null)
                {
                    result.Add(new RecurringJobDto { Id = id, Removed = true });
                    continue;
                }

                var dto = new RecurringJobDto { Id = id };

                if (hash.TryGetValue("Cron", out var cron) && !String.IsNullOrWhiteSpace(cron))
                {
                    dto.Cron = cron;
                }

                try
                {
                    if (hash.TryGetValue("Job", out var payload) && payload != null && !String.IsNullOrWhiteSpace(payload))
                    {
                        var invocationData = InvocationData.DeserializePayload(payload);
                        dto.Job = invocationData.DeserializeJob();
                    }
                }
                catch (JobLoadException ex)
                {
                    dto.LoadException = ex;
                }

                if (hash.TryGetValue("NextExecution", out var nextExecution))
                {
                    dto.NextExecution = JobHelper.DeserializeNullableDateTime(nextExecution);
                }

                if (hash.TryGetValue("LastJobId", out var lastJobId) && lastJobId != null && !string.IsNullOrWhiteSpace(lastJobId))
                {
                    dto.LastJobId = lastJobId;

                    var stateData = connection.GetStateData(lastJobId);
                    if (stateData != null)
                    {
                        dto.LastJobState = stateData.Name;
                    }
                }
                
                if (hash.TryGetValue("Queue", out var queue))
                {
                    dto.Queue = queue;
                }

                if (hash.TryGetValue("LastExecution", out var lastExecution))
                {
                    dto.LastExecution = JobHelper.DeserializeNullableDateTime(lastExecution);
                }

                if (hash.TryGetValue("TimeZoneId", out var timeZoneId))
                {
                    dto.TimeZoneId = timeZoneId;
                }

                if (hash.TryGetValue("CreatedAt", out var createdAt))
                {
                    dto.CreatedAt = JobHelper.DeserializeNullableDateTime(createdAt);
                }

                if (hash.TryGetValue("Error", out var error) && !String.IsNullOrEmpty(error))
                {
                    dto.Error = error;
                }

                if (hash.TryGetValue("RetryAttempt", out var attemptString) &&
                    Int32.TryParse(attemptString, out var retryAttempt))
                {
                    dto.RetryAttempt = retryAttempt;
                }
                else
                {
                    dto.RetryAttempt = 0;
                }

                result.Add(dto);
            }
            
            return result;
        }
    }
}
