// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Common;

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

        public static long GetRecurringJobCount([NotNull] this JobStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            return connection.GetSetCount("recurring-jobs");
        }

        public static List<RecurringJobDto> GetRecurringJobs(
            [NotNull] this JobStorageConnection connection,
            int startingFrom,
            int endingAt)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var ids = connection.GetRangeFromSet("recurring-jobs", startingFrom, endingAt);
            return GetRecurringJobDtos(connection, ids);
        }

        public static List<RecurringJobDto> GetRecurringJobs([NotNull] this IStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var ids = connection.GetAllItemsFromSet("recurring-jobs");
            return GetRecurringJobDtos(connection, ids);
        }

        public static List<RecurringJobDto> GetRecurringJobs([NotNull] this IStorageConnection connection, IEnumerable<string> ids)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
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

                var dto = new RecurringJobDto
                {
                    Id = id,
                    Cron = hash["Cron"]
                };

                try
                {
                    if (hash.TryGetValue("Job", out var payload) && !String.IsNullOrWhiteSpace(payload))
                    {
                        var invocationData = InvocationData.DeserializePayload(payload);
                        dto.Job = invocationData.DeserializeJob();
                    }
                }
                catch (JobLoadException ex)
                {
                    dto.LoadException = ex;
                }

                if (hash.ContainsKey("NextExecution"))
                {
                    dto.NextExecution = JobHelper.DeserializeNullableDateTime(hash["NextExecution"]);
                }

                if (hash.ContainsKey("LastJobId") && !string.IsNullOrWhiteSpace(hash["LastJobId"]))
                {
                    dto.LastJobId = hash["LastJobId"];

                    var stateData = connection.GetStateData(dto.LastJobId);
                    if (stateData != null)
                    {
                        dto.LastJobState = stateData.Name;
                    }
                }
                
                if (hash.ContainsKey("Queue"))
                {
                    dto.Queue = hash["Queue"];
                }

                if (hash.ContainsKey("LastExecution"))
                {
                    dto.LastExecution = JobHelper.DeserializeNullableDateTime(hash["LastExecution"]);
                }

                if (hash.ContainsKey("TimeZoneId"))
                {
                    dto.TimeZoneId = hash["TimeZoneId"];
                }

                if (hash.ContainsKey("CreatedAt"))
                {
                    dto.CreatedAt = JobHelper.DeserializeNullableDateTime(hash["CreatedAt"]);
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
