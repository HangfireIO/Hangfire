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
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Cronos;

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

        public RecurringJobManager()
            : this(JobStorage.Current)
        {
        }

        public RecurringJobManager([NotNull] JobStorage storage)
            : this(storage, new BackgroundJobFactory())
        {
        }

        public RecurringJobManager([NotNull] JobStorage storage, [NotNull] IBackgroundJobFactory factory)
            : this(storage, factory, new StateMachine(JobFilterProviders.Providers))
        {
        }

        public RecurringJobManager([NotNull] JobStorage storage, [NotNull] IBackgroundJobFactory factory, [NotNull] IStateMachine stateMachine)
            : this(storage, factory, stateMachine, () => DateTime.UtcNow)
        {
        }

        public RecurringJobManager(
            [NotNull] JobStorage storage, 
            [NotNull] IBackgroundJobFactory factory, 
            [NotNull] IStateMachine stateMachine,
            [NotNull] Func<DateTime> nowFactory)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (stateMachine == null) throw new ArgumentNullException(nameof(stateMachine));
            if (nowFactory == null) throw new ArgumentNullException(nameof(nowFactory));

            _storage = storage;
            _factory = factory;
            _stateMachine = stateMachine;
            _nowFactory = nowFactory;
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
                var recurringJob = connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
                if (recurringJob == null || recurringJob.Count == 0) recurringJob = new Dictionary<string, string>();
                var changedFields = new Dictionary<string, string>();

                var serializedJob = InvocationData.Serialize(job).Serialize();

                if (!recurringJob.ContainsKey("Job") || !recurringJob["Job"].Equals(serializedJob))
                {
                    changedFields.Add("Job", serializedJob);
                }

                if (!recurringJob.ContainsKey("Cron") || !recurringJob["Cron"].Equals(cronExpression))
                {
                    changedFields.Add("Cron", cronExpression);
                }

                if (!recurringJob.ContainsKey("TimeZoneId") || !recurringJob["TimeZoneId"].Equals(options.TimeZone.Id))
                {
                    changedFields.Add("TimeZoneId", options.TimeZone.Id);
                }

                if (!recurringJob.ContainsKey("Queue") || !recurringJob["Queue"].Equals(options.QueueName))
                {
                    changedFields.Add("Queue", options.QueueName);
                }

                if (!recurringJob.ContainsKey("CreatedAt"))
                {
                    changedFields.Add("CreatedAt", JobHelper.SerializeDateTime(DateTime.UtcNow));
                }

                var now = _nowFactory();
                var nextExecution = recurringJob.ContainsKey("NextExecution")
                    ? JobHelper.DeserializeNullableDateTime(recurringJob["NextExecution"])
                    : null;

                if (!nextExecution.HasValue || nextExecution > now)
                {
                    var expression = CronExpression.Parse(cronExpression);
                    var oldExecution = nextExecution;
                    nextExecution = expression.GetNextOccurrence(now, options.TimeZone, inclusive: true);

                    if (!recurringJob.ContainsKey("NextExecution") || oldExecution != nextExecution)
                    {
                        changedFields.Add("NextExecution", nextExecution.HasValue
                            ? JobHelper.SerializeDateTime(nextExecution.Value)
                            : String.Empty);
                    }
                }

                if (changedFields.Count > 0)
                {
                    using (var transaction = connection.CreateWriteTransaction())
                    {
                        transaction.SetRangeInHash($"recurring-job:{recurringJobId}", changedFields);

                        transaction.AddToSet(
                            "recurring-jobs",
                            recurringJobId,
                            nextExecution.HasValue ? JobHelper.ToTimestamp(nextExecution.Value) : double.MaxValue);

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
                var recurringJob = connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
                if (recurringJob == null) return;

                var changedFields = new Dictionary<string, string>();
                var job = InvocationData.Deserialize(recurringJob["Job"]).Deserialize();
                var state = new EnqueuedState { Reason = "Triggered using recurring job manager" };

                if (recurringJob.ContainsKey("Queue"))
                {
                    state.Queue = recurringJob["Queue"];
                }

                var context = new CreateContext(_storage, connection, job, null);
                context.Parameters["RecurringJobId"] = recurringJobId;

                var backgroundJob = _factory.Create(context);

                if (backgroundJob != null)
                {
                    var now = _nowFactory();

                    var timeZone = recurringJob.ContainsKey("TimeZoneId")
                        ? TimeZoneInfo.FindSystemTimeZoneById(recurringJob["TimeZoneId"])
                        : TimeZoneInfo.Utc;

                    var nextExecution = CronExpression.Parse(recurringJob["Cron"]).GetNextOccurrence(
                        now,
                        timeZone,
                        inclusive: false);

                    changedFields.Add("LastExecution", JobHelper.SerializeDateTime(now));
                    changedFields.Add("LastJobId", backgroundJob.Id);
                    changedFields.Add("NextExecution", nextExecution.HasValue ? JobHelper.SerializeDateTime(nextExecution.Value) : String.Empty);

                    using (var transaction = connection.CreateWriteTransaction())
                    {
                        _stateMachine.ApplyState(new ApplyStateContext(
                            context.Storage,
                            connection,
                            transaction,
                            backgroundJob,
                            state,
                            null));

                        transaction.SetRangeInHash($"recurring-job:{recurringJobId}", changedFields);
                        transaction.AddToSet(
                            "recurring-jobs",
                            recurringJobId,
                            nextExecution.HasValue ? JobHelper.ToTimestamp(nextExecution.Value) : double.MaxValue);

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

        private static void ValidateCronExpression(string cronExpression)
        {
            try
            {
                var expression = CronExpression.Parse(cronExpression);
                expression.GetNextOccurrence(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("CRON expression is invalid. Please see the inner exception for details.", nameof(cronExpression), ex);
            }
        }
    }
}
