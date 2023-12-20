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
using Hangfire.Storage;

namespace Hangfire
{
    internal class RecurringJobEntity
    {
        private static readonly char[] SeparatorCharacters = new[] { ' ', '\t' };

        private readonly IList<Exception> _errors = new List<Exception>();
        private readonly IDictionary<string, string> _recurringJob;
        private readonly DateTime _now;

        public RecurringJobEntity(
            [NotNull] string recurringJobId,
            [NotNull] IDictionary<string, string> recurringJob,
            [NotNull] ITimeZoneResolver timeZoneResolver,
            DateTime now)
        {
            if (timeZoneResolver == null) throw new ArgumentNullException(nameof(timeZoneResolver));

            _recurringJob = recurringJob ?? throw new ArgumentNullException(nameof(recurringJob));
            _now = now;

            RecurringJobId = recurringJobId ?? throw new ArgumentNullException(nameof(recurringJobId));

            if (recurringJob.TryGetValue("Queue", out var queue) && !String.IsNullOrWhiteSpace(queue))
            {
                Queue = queue;
            }

            try
            {
                TimeZone = recurringJob.TryGetValue("TimeZoneId", out var timeZoneId) && !String.IsNullOrWhiteSpace(timeZoneId)
                    ? timeZoneResolver.GetTimeZoneById(timeZoneId)
                    : TimeZoneInfo.Utc;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                _errors.Add(ex);
            }

            if (recurringJob.TryGetValue("Cron", out var cron) && !String.IsNullOrWhiteSpace(cron))
            {
                Cron = cron;
            }

            try
            {
                if (!recurringJob.TryGetValue("Job", out var job) || String.IsNullOrWhiteSpace(job))
                {
                    throw new InvalidOperationException("The 'Job' field has a null or empty value");
                }

                Job = InvocationData.DeserializePayload(job).DeserializeJob();
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                _errors.Add(ex);
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

            if (recurringJob.TryGetValue("CreatedAt", out string createdAt) && !String.IsNullOrWhiteSpace(createdAt))
            {
                CreatedAt = JobHelper.DeserializeDateTime(createdAt);
            }
            else
            {
                CreatedAt = now;
            }

            if (recurringJob.TryGetValue("Misfire", out var misfireStr))
            {
                MisfireHandling = (MisfireHandlingMode)Enum.Parse(typeof(MisfireHandlingMode), misfireStr);
                if (!Enum.IsDefined(typeof(MisfireHandlingMode), MisfireHandling))
                {
                    throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, "Misfire option '{0}' is not supported.", (int)MisfireHandling));
                }
            }
            else
            {
                MisfireHandling = MisfireHandlingMode.Relaxed;
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
        }

        public string RecurringJobId { get; }

        public string Queue { get; set; }
        public string Cron { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
        public Job Job { get; set; }
        public MisfireHandlingMode MisfireHandling { get; set; }

        public DateTime CreatedAt { get; }
        public DateTime? NextExecution { get; private set; }

        public DateTime? LastExecution { get; set; }
        public string LastJobId { get; set; }
        public int? Version { get; set; }
        public int RetryAttempt { get; set; }

        public Exception[] Errors => _errors.ToArray();

        public IEnumerable<DateTime> TrySchedule(DateTime now, TimeSpan precision, out Exception error)
        {
            if (_errors.Count > 0)
            {
                error = _errors.Count == 1 ? _errors[0] : new AggregateException(_errors);
                return Enumerable.Empty<DateTime>();
            }

            var result = new List<DateTime>();
            DateTime? nextExecution = null;

            while (TryGetNextExecution(nextExecution, out nextExecution, out error))
            {
                if (nextExecution == null || nextExecution > now) break;
                if (nextExecution == now)
                {
                    result.Add(nextExecution.Value);
                }
                else
                {
                    switch (MisfireHandling)
                    {
                        case MisfireHandlingMode.Relaxed:
                            nextExecution = now;
                            result.Add(nextExecution.Value);
                            break;
                        case MisfireHandlingMode.Strict:
                            result.Add(nextExecution.Value);
                            break;
                        case MisfireHandlingMode.Ignorable:
                            if (now.Add(precision.Negate()) <= nextExecution && nextExecution <= now)
                            {
                                result.Add(nextExecution.Value);
                            }

                            break;
                    }
                }
            }

            NextExecution = nextExecution;
            return result;
        }

        public bool IsChanged(out IReadOnlyDictionary<string, string> changedFields, out DateTime? nextExecution)
        {
            changedFields = GetChangedFields(out nextExecution);
            return changedFields.Count > 0 || nextExecution != NextExecution;
        }

        public void ScheduleRetry(TimeSpan delay, string error, out IReadOnlyDictionary<string, string> changedFields, out DateTime? nextExecution)
        {
            RetryAttempt++;
            nextExecution = _now.Add(delay);

            var result = new Dictionary<string, string>
            {
                { "RetryAttempt", RetryAttempt.ToString(CultureInfo.InvariantCulture) },
                { "Error", error ?? String.Empty }
            };
            
            if (!_recurringJob.ContainsKey("V"))
            {
                result.Add("V", "2");
            }

            changedFields = result;
        }

        public void Disable(string error, out IReadOnlyDictionary<string, string> changedFields, out DateTime? nextExecution)
        {
            nextExecution = null;

            var result = new Dictionary<string, string>
            {
                { "NextExecution", String.Empty },
                { "Error", error ?? String.Empty }
            };

            if (!_recurringJob.ContainsKey("V"))
            {
                result.Add("V", "2");
            }

            changedFields = result;
        }

        private IReadOnlyDictionary<string, string> GetChangedFields(out DateTime? nextExecution)
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

            var timeZoneChanged = !TimeZone.Id.Equals(
                _recurringJob.TryGetValue("TimeZoneId", out var tz)
                    ? tz
                    : TimeZoneInfo.Utc.Id,
                StringComparison.Ordinal);

            var serializedNextExecution = NextExecution.HasValue ? JobHelper.SerializeDateTime(NextExecution.Value) : null;
            if (serializedNextExecution != null &&
                (_recurringJob.TryGetValue("NextExecution", out var next) ? next : null) !=
                serializedNextExecution)
            {
                result.Add("NextExecution", serializedNextExecution);
                nextExecution = NextExecution;
            }
            else
            {
                TryGetNextExecution(result.ContainsKey("Cron") || timeZoneChanged ? _now.AddSeconds(-1) : (DateTime?)null, out nextExecution, out _);
                serializedNextExecution = nextExecution.HasValue ? JobHelper.SerializeDateTime(nextExecution.Value) : null;

                if ((_recurringJob.TryGetValue("NextExecution", out var next2) ? next2 : null) !=
                    serializedNextExecution)
                {
                    result.Add("NextExecution", serializedNextExecution ?? String.Empty);
                }
            }

            if ((_recurringJob.TryGetValue("LastJobId", out var last) ? last : null) != LastJobId)
            {
                result.Add("LastJobId", LastJobId ?? String.Empty);
            }

            var misfireHandlingValue = MisfireHandling.ToString("D");
            if ((!_recurringJob.ContainsKey("Misfire") && MisfireHandling != MisfireHandlingMode.Relaxed) ||
                (_recurringJob.TryGetValue("Misfire", out var misfire) && misfire != misfireHandlingValue))
            {
                result.Add("Misfire", misfireHandlingValue);
            }

            if (!_recurringJob.ContainsKey("V"))
            {
                result.Add("V", "2");
            }

            if (_recurringJob.TryGetValue("Error", out var error) && !String.IsNullOrEmpty(error))
            {
                result.Add("Error", String.Empty);
            }

            if (_recurringJob.TryGetValue("RetryAttempt", out var retryAttempt) && retryAttempt != "0")
            {
                result.Add("RetryAttempt", "0");
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

        private bool TryGetNextExecution(DateTime? from, out DateTime? nextExecution, out Exception exception)
        {
            try
            {
                nextExecution = ParseCronExpression(Cron)?.GetNextOccurrence(
                    from ?? (LastExecution ?? CreatedAt.AddSeconds(-1)),
                    TimeZone,
                    inclusive: false);

                exception = null;
                return true;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                exception = ex;
                nextExecution = null;
                return false;
            }
        }
    }
}