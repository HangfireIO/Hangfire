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
using System.Globalization;
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

            if (recurringJob.TryGetValue("Queue", out var queue) && !String.IsNullOrWhiteSpace(queue))
            {
                Queue = queue;
            }

            TimeZone = recurringJob.TryGetValue("TimeZoneId", out var timeZoneId) && !String.IsNullOrWhiteSpace(timeZoneId)
                ? timeZoneResolver.GetTimeZoneById(timeZoneId)
                : TimeZoneInfo.Utc;

            if (recurringJob.TryGetValue("Cron", out var cron) && !String.IsNullOrWhiteSpace(cron))
            {
                Cron = cron;
            }

            if (recurringJob.TryGetValue("Job", out var job) && !String.IsNullOrWhiteSpace(job))
            {
                Job = InvocationData.DeserializePayload(job).DeserializeJob();
            }

            if (recurringJob.TryGetValue("LastJobId", out var lastJobId) && !String.IsNullOrWhiteSpace(lastJobId))
            {
                LastJobId = lastJobId;
            }

            if (recurringJob.TryGetValue("LastExecution", out var lastExecution) && !String.IsNullOrWhiteSpace(lastExecution))
            {
                LastExecution = JobHelper.DeserializeDateTime(lastExecution);
            }

            if (recurringJob.TryGetValue("NextExecution", out var nextExecution) && !String.IsNullOrWhiteSpace(nextExecution))
            {
                NextExecution = JobHelper.DeserializeDateTime(nextExecution);
            }

            if (recurringJob.TryGetValue("CreatedAt", out var createdAt) && !String.IsNullOrWhiteSpace(createdAt))
            {
                CreatedAt = JobHelper.DeserializeDateTime(createdAt);
            }
            else
            {
                CreatedAt = now;
            }

            if (recurringJob.TryGetValue("V", out var v) && !String.IsNullOrWhiteSpace(v))
            {
                Version = int.Parse(v, CultureInfo.InvariantCulture);
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
        public int? Version { get; set; }

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

            if ((_recurringJob.TryGetValue("Queue", out var queue) ? queue : null) != Queue)
            {
                result.Add("Queue", Queue);
            }

            if ((_recurringJob.TryGetValue("Cron", out var cron) ? cron : null) != Cron)
            {
                result.Add("Cron", Cron);
            }

            if ((_recurringJob.TryGetValue("TimeZoneId", out var timeZoneId) ? timeZoneId : null) != TimeZone.Id)
            {
                result.Add("TimeZoneId", TimeZone.Id);
            }

            var serializedJob = InvocationData.SerializeJob(Job).SerializePayload();

            if ((_recurringJob.TryGetValue("Job", out var job) ? job : null) != serializedJob)
            {
                result.Add("Job", serializedJob);
            }

            var serializedCreatedAt = JobHelper.SerializeDateTime(CreatedAt);

            if ((_recurringJob.TryGetValue("CreatedAt", out var createdAt) ? createdAt : null) != serializedCreatedAt)
            {
                result.Add("CreatedAt", serializedCreatedAt);
            }

            var serializedLastExecution = LastExecution.HasValue ? JobHelper.SerializeDateTime(LastExecution.Value) : null;

            if ((_recurringJob.TryGetValue("LastExecution", out var lastExecution) ? lastExecution : null) !=
                serializedLastExecution)
            {
                result.Add("LastExecution", serializedLastExecution ?? String.Empty);
            }

            nextExecution = GetNextExecution();
            var serializedNextExecution = nextExecution.HasValue ? JobHelper.SerializeDateTime(nextExecution.Value) : null;

            if ((_recurringJob.TryGetValue("NextExecution", out var nextExecutionValue) ? nextExecutionValue : null) !=
                serializedNextExecution)
            {
                result.Add("NextExecution", serializedNextExecution ?? String.Empty);
            }

            if ((_recurringJob.TryGetValue("LastJobId", out var lastJobId) ? lastJobId : null) != LastJobId)
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