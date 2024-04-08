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
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Profiling;
using Hangfire.Storage;

namespace Hangfire
{
    /// <summary>
    /// Represents a recurring job manager that allows to create, update
    /// or delete recurring jobs.
    /// </summary>
    public class RecurringJobManager : IRecurringJobManager
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        private readonly ILog _logger = LogProvider.GetLogger(typeof(RecurringJobManager));

        private readonly JobStorage _storage;
        private readonly IBackgroundJobFactory _factory;
        private readonly Func<DateTime> _nowFactory;
        private readonly ITimeZoneResolver _timeZoneResolver;

        public RecurringJobManager()
            : this(JobStorage.Current)
        {
        }

        public RecurringJobManager([NotNull] JobStorage storage)
            : this(storage, JobFilterProviders.Providers)
        {
        }

        public RecurringJobManager([NotNull] JobStorage storage, [NotNull] IJobFilterProvider filterProvider)
            : this(storage, filterProvider, new DefaultTimeZoneResolver())
        {
        }

        public RecurringJobManager(
            [NotNull] JobStorage storage, 
            [NotNull] IJobFilterProvider filterProvider,
            [NotNull] ITimeZoneResolver timeZoneResolver)
            : this(storage, filterProvider, timeZoneResolver, () => DateTime.UtcNow)
        {
        }

        public RecurringJobManager(
            [NotNull] JobStorage storage, 
            [NotNull] IJobFilterProvider filterProvider, 
            [NotNull] ITimeZoneResolver timeZoneResolver,
            [NotNull] Func<DateTime> nowFactory)
            : this(storage, new BackgroundJobFactory(filterProvider), timeZoneResolver, nowFactory)
        {
        }

        public RecurringJobManager([NotNull] JobStorage storage, [NotNull] IBackgroundJobFactory factory)
            : this(storage, factory, new DefaultTimeZoneResolver())
        {
        }

        public RecurringJobManager([NotNull] JobStorage storage, [NotNull] IBackgroundJobFactory factory, [NotNull] ITimeZoneResolver timeZoneResolver)
            : this(storage, factory, timeZoneResolver, () => DateTime.UtcNow)
        {
        }

        internal RecurringJobManager(
            [NotNull] JobStorage storage, 
            [NotNull] IBackgroundJobFactory factory,
            [NotNull] ITimeZoneResolver timeZoneResolver,
            [NotNull] Func<DateTime> nowFactory)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _timeZoneResolver = timeZoneResolver ?? throw new ArgumentNullException(nameof(timeZoneResolver));
            _nowFactory = nowFactory ?? throw new ArgumentNullException(nameof(nowFactory));
        }

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            if (options == null) throw new ArgumentNullException(nameof(options));

            ValidateCronExpression(cronExpression);

            using (var connection = _storage.GetConnection())
            using (connection.AcquireDistributedRecurringJobLock(recurringJobId, DefaultTimeout))
            {
                var now = _nowFactory();
                var recurringJob = connection.GetOrCreateRecurringJob(recurringJobId);
                var scheduleChanged = false;

                recurringJob.Job = InvocationData.SerializeJob(job).SerializePayload();

                if (!cronExpression.Equals(recurringJob.Cron, StringComparison.OrdinalIgnoreCase))
                {
                    recurringJob.Cron = cronExpression;
                    scheduleChanged = true;
                }

                if (!options.TimeZone.Id.Equals(recurringJob.TimeZoneId, StringComparison.OrdinalIgnoreCase))
                {
                    recurringJob.TimeZoneId = options.TimeZone.Id;
                    scheduleChanged = true;
                }

                recurringJob.Queue = options.QueueName;
                recurringJob.RetryAttempt = 0;

                if (scheduleChanged || recurringJob.Error != null)
                {
                    recurringJob.ScheduleNext(_timeZoneResolver, now.AddSeconds(-1));
                }

                if (recurringJob.IsChanged(now, out var changedFields))
                {
                    using (var transaction = connection.CreateWriteTransaction())
                    {
                        transaction.UpdateRecurringJob(recurringJob, changedFields, _logger);
                        transaction.Commit();
                    }
                }
            }
        }
 
        private static void ValidateCronExpression(string cronExpression)
        {
            try
            {
                RecurringJobEntity.ParseCronExpression(cronExpression);
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                throw new ArgumentException(
                    "CRON expression is invalid. Please see the inner exception for details.",
                    nameof(cronExpression),
                    ex);
            }
        }

        public void Trigger(string recurringJobId)
        {
            TriggerJob(recurringJobId);
        }

        [Obsolete("Please use the TriggerJob method with the same arguments instead. Will be removed in 2.0.0.")]
        public string TriggerExecution([NotNull] string recurringJobId)
        {
            return TriggerJob(recurringJobId);
        }

        public string TriggerJob([NotNull] string recurringJobId)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));

            using (var connection = _storage.GetConnection())
            using (connection.AcquireDistributedRecurringJobLock(recurringJobId, DefaultTimeout))
            {
                var now = _nowFactory();

                var recurringJob = connection.GetRecurringJob(recurringJobId);
                if (recurringJob == null) return null;

                BackgroundJob backgroundJob;

                try
                {
                    backgroundJob = _factory.TriggerRecurringJob(_storage, connection, EmptyProfiler.Instance, recurringJob, now);
                    recurringJob.ScheduleNext(_timeZoneResolver, now);
                }
                catch (Exception ex) when (ex.IsCatchableExceptionType())
                {
                    // TODO: Preserving backward compatibility, should be removed in 2.0.0.
                    throw new AggregateException(ex);
                }

                if (recurringJob.IsChanged(now, out var changedFields))
                {
                    using (var transaction = connection.CreateWriteTransaction())
                    {
                        if (backgroundJob != null)
                        {
                            _factory.StateMachine.EnqueueBackgroundJob(
                                _storage,
                                connection,
                                transaction,
                                recurringJob,
                                backgroundJob,
                                "Triggered using recurring job manager",
                                EmptyProfiler.Instance);
                        }

                        transaction.UpdateRecurringJob(recurringJob, changedFields, _logger);
                        transaction.Commit();
                    }

                    return backgroundJob?.Id;
                }

                return null;
            }
        }

        public void RemoveIfExists(string recurringJobId)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));

            using (var connection = _storage.GetConnection())
            using (connection.AcquireDistributedRecurringJobLock(recurringJobId, DefaultTimeout))
            using (var transaction = connection.CreateWriteTransaction())
            {
                transaction.RemoveHash($"recurring-job:{recurringJobId}");
                transaction.RemoveFromSet("recurring-jobs", recurringJobId);

                transaction.Commit();
            }
        }
    }
}
