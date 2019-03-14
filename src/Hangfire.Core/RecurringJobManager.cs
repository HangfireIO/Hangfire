// This file is part of Hangfire.
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
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Profiling;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Represents a recurring job manager that allows to create, update
    /// or delete recurring jobs.
    /// </summary>
    public class RecurringJobManager : IRecurringJobManager
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        private readonly JobStorage _storage;
        private readonly IBackgroundJobFactory _factory;
        private readonly IStateMachine _stateMachine;
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
#pragma warning disable 618
            : this(storage, new BackgroundJobFactory(filterProvider), new StateMachine(filterProvider), timeZoneResolver, nowFactory)
#pragma warning restore 618
        {
        }

        [Obsolete("Please use RecurringJobManager(JobStorage, IJobFilterProvider) overload instead. Will be removed in 2.0.0.")]
        public RecurringJobManager([NotNull] JobStorage storage, [NotNull] IBackgroundJobFactory factory)
            : this(storage, factory, new StateMachine(JobFilterProviders.Providers))
        {
        }

        [Obsolete("Please use RecurringJobManager(JobStorage, IJobFilterProvider) overload instead. Will be removed in 2.0.0.")]
        public RecurringJobManager([NotNull] JobStorage storage, [NotNull] IBackgroundJobFactory factory, [NotNull] IStateMachine stateMachine)
            : this(storage, factory, stateMachine, new DefaultTimeZoneResolver(), () => DateTime.UtcNow)
        {
        }

        internal RecurringJobManager(
            [NotNull] JobStorage storage, 
            [NotNull] IBackgroundJobFactory factory, 
            [NotNull] IStateMachine stateMachine,
            [NotNull] ITimeZoneResolver timeZoneResolver,
            [NotNull] Func<DateTime> nowFactory)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _timeZoneResolver = timeZoneResolver ?? throw new ArgumentNullException(nameof(timeZoneResolver));
            _nowFactory = nowFactory ?? throw new ArgumentNullException(nameof(nowFactory));
        }

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            if (options == null) throw new ArgumentNullException(nameof(options));

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
                        transaction.UpdateRecurringJob(recurringJobId, changedFields, nextExecution);
                        transaction.Commit();
                    }
                }
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

                var backgroundJob = _factory.TriggerRecurringJob(_storage, connection, EmptyProfiler.Instance, recurringJob, now);

                if (recurringJob.IsChanged(out var changedFields, out var nextExecution))
                {
                    using (var transaction = connection.CreateWriteTransaction())
                    {
                        if (backgroundJob != null)
                        {
                            _stateMachine.EnqueueBackgroundJob(
                                _storage,
                                connection,
                                transaction,
                                recurringJob,
                                backgroundJob,
                                "Triggered using recurring job manager",
                                EmptyProfiler.Instance);
                        }

                        transaction.UpdateRecurringJob(recurringJobId, changedFields, nextExecution);

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
