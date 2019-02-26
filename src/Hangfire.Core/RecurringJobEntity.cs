// This file is part of Hangfire.
// Copyright © 2019 Sergey Odinokov.
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
using Cronos;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire
{
    internal class RecurringJobEntity
    {
        private readonly IDictionary<string, string> _recurringJob;

        public RecurringJobEntity(
            [NotNull] string recurringJobId,
            [NotNull] IDictionary<string, string> recurringJob,
            [NotNull] ITimeZoneResolver timeZoneResolver,
            DateTime now)
        {
            if (timeZoneResolver == null) throw new ArgumentNullException(nameof(timeZoneResolver));

            _recurringJob = recurringJob ?? throw new ArgumentNullException(nameof(recurringJob));

            RecurringJobId = recurringJobId ?? throw new ArgumentNullException(nameof(recurringJobId));

            if (recurringJob.ContainsKey("Queue") && !String.IsNullOrWhiteSpace(recurringJob["Queue"]))
            {
                Queue = recurringJob["Queue"];
            }

            TimeZone = recurringJob.ContainsKey("TimeZoneId") && !String.IsNullOrWhiteSpace("TimeZoneId")
                ? timeZoneResolver.GetTimeZoneById(recurringJob["TimeZoneId"])
                : TimeZoneInfo.Utc;

            if (recurringJob.ContainsKey("Cron") && !String.IsNullOrWhiteSpace(recurringJob["Cron"]))
            {
                Cron = recurringJob["Cron"];
            }

            if (recurringJob.ContainsKey("Job") && !String.IsNullOrWhiteSpace(recurringJob["Job"]))
            {
                Job = InvocationData.DeserializePayload(recurringJob["Job"]).DeserializeJob();
            }

            if (recurringJob.ContainsKey("LastJobId") && !String.IsNullOrWhiteSpace(recurringJob["LastJobId"]))
            {
                LastJobId = recurringJob["LastJobId"];
            }

            if (recurringJob.ContainsKey("LastExecution") && !String.IsNullOrWhiteSpace(recurringJob["LastExecution"]))
            {
                LastExecution = JobHelper.DeserializeDateTime(recurringJob["LastExecution"]);
            }

            if (recurringJob.ContainsKey("NextExecution") && !String.IsNullOrWhiteSpace(recurringJob["NextExecution"]))
            {
                NextExecution = JobHelper.DeserializeDateTime(recurringJob["NextExecution"]);
            }

            if (recurringJob.ContainsKey("CreatedAt") && !String.IsNullOrWhiteSpace(recurringJob["CreatedAt"]))
            {
                CreatedAt = JobHelper.DeserializeDateTime(recurringJob["CreatedAt"]);
            }
            else
            {
                CreatedAt = now;
            }
        }

        public string RecurringJobId { get; }

        public string Queue { get; set; }
        public string Cron { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
        public Job Job { get; set; }

        public DateTime CreatedAt { get; }
        public DateTime? NextExecution { get; }

        public DateTime? LastExecution { get; set; }
        public string LastJobId { get; set; }

        public DateTime? GetNextExecution()
        {
            var parts = Cron.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var format = CronFormat.Standard;

            try
            {
                if (parts.Length == 6)
                {
                    format |= CronFormat.IncludeSeconds;
                }
                else if (parts.Length != 5)
                {
                    throw new CronFormatException(
                        $"Wrong number of parts in the `{Cron}` cron expression, you can only use 5 or 6 (with seconds) part-based expressions.");
                }

                return CronExpression.Parse(Cron, format).GetNextOccurrence(
                    LastExecution ?? CreatedAt.AddSeconds(-1),
                    TimeZone,
                    inclusive: false);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("CRON expression is invalid. Please see the inner exception for details.", "cronExpression", ex);
            }
        }

        public bool IsChanged(out IReadOnlyDictionary<string, string> changedFields, out DateTime? nextExecution)
        {
            changedFields = GetChangedFields(out nextExecution);
            return changedFields.Count > 0 || nextExecution != NextExecution;
        }

        public IReadOnlyDictionary<string, string> GetChangedFields(out DateTime? nextExecution)
        {
            var result = new Dictionary<string, string>();

            if ((_recurringJob.ContainsKey("Queue") ? _recurringJob["Queue"] : null) != Queue)
            {
                result.Add("Queue", Queue);
            }

            if ((_recurringJob.ContainsKey("Cron") ? _recurringJob["Cron"] : null) != Cron)
            {
                result.Add("Cron", Cron);
            }

            if ((_recurringJob.ContainsKey("TimeZoneId") ? _recurringJob["TimeZoneId"] : null) != TimeZone.Id)
            {
                result.Add("TimeZoneId", TimeZone.Id);
            }

            var serializedJob = InvocationData.SerializeJob(Job).SerializePayload();

            if ((_recurringJob.ContainsKey("Job") ? _recurringJob["Job"] : null) != serializedJob)
            {
                result.Add("Job", serializedJob);
            }

            var serializedCreatedAt = JobHelper.SerializeDateTime(CreatedAt);

            if ((_recurringJob.ContainsKey("CreatedAt") ? _recurringJob["CreatedAt"] : null) != serializedCreatedAt)
            {
                result.Add("CreatedAt", serializedCreatedAt);
            }

            var serializedLastExecution = LastExecution.HasValue ? JobHelper.SerializeDateTime(LastExecution.Value) : null;

            if ((_recurringJob.ContainsKey("LastExecution") ? _recurringJob["LastExecution"] : null) !=
                serializedLastExecution)
            {
                result.Add("LastExecution", serializedLastExecution ?? String.Empty);
            }

            nextExecution = GetNextExecution();
            var serializedNextExecution = nextExecution.HasValue ? JobHelper.SerializeDateTime(nextExecution.Value) : null;

            if ((_recurringJob.ContainsKey("NextExecution") ? _recurringJob["NextExecution"] : null) !=
                serializedNextExecution)
            {
                result.Add("NextExecution", serializedNextExecution ?? String.Empty);
            }

            if ((_recurringJob.ContainsKey("LastJobId") ? _recurringJob["LastJobId"] : null) != LastJobId)
            {
                result.Add("LastJobId", LastJobId ?? String.Empty);
            }

            if (!_recurringJob.ContainsKey("V"))
            {
                result.Add("V", "2");
            }

            return result;
        }
    }
}