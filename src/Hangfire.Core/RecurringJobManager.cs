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
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Profiling;

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
                var recurringJob = connection.GetOrCreateRecurringJob(recurringJobId, _timeZoneResolver, _nowFactory());

                recurringJob.Job = job;
                recurringJob.Cron = cronExpression;
                recurringJob.TimeZone = options.TimeZone;
                recurringJob.Queue = options.QueueName;

                if (recurringJob.IsChanged(out var changedFields, out var nextExecution))
                {
                    using (var transaction = connection.CreateWriteTransaction())
                    {
                        transaction.UpdateRecurringJob(recurringJob, changedFields, nextExecution, _logger);
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
            catch (Exception ex)
            {
                throw new ArgumentException(
                    "CRON expression is invalid. Please see the inner exception for details.",
                    nameof(cronExpression),
                    ex);
            }
        }

        public void Trigger(string recurringJobId)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));

            using (var connection = _storage.GetConnection())
            using (connection.AcquireDistributedRecurringJobLock(recurringJobId, DefaultTimeout))
            {
                var now = _nowFactory();

                var recurringJob = connection.GetRecurringJob(recurringJobId, _timeZoneResolver, now);
                if (recurringJob == null) return;

                if (recurringJob.Errors.Length > 0)
                {
                    throw new AggregateException($"Can't trigger recurring job '{recurringJobId}' due to errors", recurringJob.Errors);
                }

                var backgroundJob = _factory.TriggerRecurringJob(_storage, connection, EmptyProfiler.Instance, recurringJob, now);

                if (recurringJob.IsChanged(out var changedFields, out var nextExecution))
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

                        transaction.UpdateRecurringJob(recurringJob, changedFields, nextExecution, _logger);
                        transaction.Commit();
                    }
                }
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
