// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
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
using System.Linq;
using Cronos;
using Hangfire.Annotations;
using Hangfire.Common;

namespace Hangfire
{
    internal class RecurringJobEntity
    {
        private static readonly char[] SeparatorCharacters = new[] { ' ', '\t' };

        private readonly IDictionary<string, string> _recurringJob;

        public RecurringJobEntity(
            [NotNull] string recurringJobId,
            [NotNull] IDictionary<string, string> recurringJob)
        {
            _recurringJob = recurringJob ?? throw new ArgumentNullException(nameof(recurringJob));

            RecurringJobId = recurringJobId ?? throw new ArgumentNullException(nameof(recurringJobId));

            if (recurringJob.TryGetValue("Queue", out var queue) && !String.IsNullOrWhiteSpace(queue))
            {
                Queue = queue;
            }

            if (recurringJob.TryGetValue("TimeZoneId", out var timeZoneId) && !String.IsNullOrWhiteSpace(timeZoneId))
            {
                TimeZoneId = timeZoneId;
            }
            else
            {
                TimeZoneId = TimeZoneInfo.Utc.Id;
            }

            if (recurringJob.TryGetValue("Cron", out var cron) && !String.IsNullOrWhiteSpace(cron))
            {
                Cron = cron;
            }
            
            if (recurringJob.TryGetValue("Job", out var job) && !String.IsNullOrWhiteSpace(job))
            {
                Job = job;
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

            if (recurringJob.TryGetValue("V", out var version) && !String.IsNullOrWhiteSpace(version))
            {
                Version = int.Parse(version, CultureInfo.InvariantCulture);
            }

            if (recurringJob.TryGetValue("RetryAttempt", out var attemptString) &&
                int.TryParse(attemptString, out var retryAttempt))
            {
                RetryAttempt = retryAttempt;
            }

            if (recurringJob.TryGetValue("Error", out var error) && !String.IsNullOrWhiteSpace(error))
            {
                Error = error;
            }
        }

        public string RecurringJobId { get; }

        public string Queue { get; set; }
        public string Cron { get; set; }
        public string TimeZoneId { get; set; }
        public string Job { get; set; }

        public DateTime? CreatedAt { get; }
        public DateTime? NextExecution { get; private set; }

        public DateTime? LastExecution { get; set; }
        public string LastJobId { get; set; }
        public int? Version { get; private set; }
        public int RetryAttempt { get; set; }
        public string Error { get; set; }

        public void ScheduleNext(ITimeZoneResolver timeZoneResolver, DateTime from)
        {
            ScheduleNext(timeZoneResolver, from, from, TimeSpan.Zero);
        }

        public IEnumerable<DateTime> ScheduleNext(
            ITimeZoneResolver timeZoneResolver,
            DateTime from,
            DateTime now,
            TimeSpan precision)
        {
            if (timeZoneResolver == null) throw new ArgumentNullException(nameof(timeZoneResolver));

            var result = new List<DateTime>();
            var cron = ParseCronExpression(Cron);
            var timeZone = timeZoneResolver.GetTimeZoneById(TimeZoneId);

            DateTime? next = from;

            while ((next = cron.GetNextOccurrence(next.Value, timeZone, inclusive: false)) <= now)
            {
                if (next == now)
                {
                    result.Add(next.Value);
                }
                else
                {
                    next = now;
                    result.Add(next.Value);
                }
            }

            NextExecution = next;
            Error = null;
            return result;
        }

        public bool IsChanged(DateTime now, out IReadOnlyDictionary<string, string> changedFields)
        {
            changedFields = GetChangedFields(now);
            return changedFields.Count > 0;
        }

        public void ScheduleRetry(DateTime nextAttempt, string error)
        {
            RetryAttempt++;
            Error = error;
            NextExecution = nextAttempt;
        }

        public void Disable(string error)
        {
            NextExecution = null;
            Error = error;
        }

        private IReadOnlyDictionary<string, string> GetChangedFields(DateTime now)
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

            if ((_recurringJob.TryGetValue("TimeZoneId", out var timeZoneId) ? timeZoneId : null) != TimeZoneId)
            {
                result.Add("TimeZoneId", TimeZoneId);
            }

            if ((_recurringJob.TryGetValue("Job", out var job) ? job : null) != Job)
            {
                result.Add("Job", Job);
            }

            if (!_recurringJob.ContainsKey("CreatedAt"))
            {
                result.Add("CreatedAt", JobHelper.SerializeDateTime(now));
            }

            var serializedLastExecution = LastExecution.HasValue ? JobHelper.SerializeDateTime(LastExecution.Value) : null;

            if ((_recurringJob.TryGetValue("LastExecution", out var lastExecution) ? lastExecution : null) !=
                serializedLastExecution)
            {
                result.Add("LastExecution", serializedLastExecution ?? String.Empty);
            }

            var serializedNextExecution = NextExecution.HasValue ? JobHelper.SerializeDateTime(NextExecution.Value) : null;
            if ((_recurringJob.TryGetValue("NextExecution", out var next) ? next : null) !=
                serializedNextExecution)
            {
                result.Add("NextExecution", serializedNextExecution ?? String.Empty);
            }

            if ((_recurringJob.TryGetValue("LastJobId", out var last) ? last : null) != LastJobId)
            {
                result.Add("LastJobId", LastJobId ?? String.Empty);
            }

            if (!_recurringJob.ContainsKey("V"))
            {
                result.Add("V", "2");
            }

            if ((_recurringJob.TryGetValue("Error", out var error) ? error : null) != Error)
            {
                result.Add("Error", Error ?? String.Empty);
            }

            var retryAttemptValue = RetryAttempt.ToString(CultureInfo.InvariantCulture);
            if ((_recurringJob.TryGetValue("RetryAttempt", out var retryAttempt) ? retryAttempt : null) != retryAttemptValue)
            {
                if (_recurringJob.ContainsKey("RetryAttempt") || retryAttemptValue != "0")
                {
                    result.Add("RetryAttempt", retryAttemptValue);
                }
            }

            return result;
        }

        public override string ToString()
        {
            return String.Join(";", _recurringJob.Select(x => $"{x.Key}:{x.Value}"));
        }

        public static CronExpression ParseCronExpression([NotNull] string cronExpression)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));

            var format = CronFormat.Standard;

            if (!cronExpression.StartsWith("@", StringComparison.OrdinalIgnoreCase))
            {
                var parts = cronExpression.Split(SeparatorCharacters, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 6)
                {
                    format |= CronFormat.IncludeSeconds;
                }
                else if (parts.Length != 5)
                {
                    throw new CronFormatException(
                        $"Wrong number of parts in the `{cronExpression}` cron expression, you can only use 5 or 6 (with seconds) part-based expressions.");
                }
            }

            return CronExpression.Parse(cronExpression, format);
        }
    }
}