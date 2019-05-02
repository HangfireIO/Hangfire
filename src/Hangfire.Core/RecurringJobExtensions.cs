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
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Profiling;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire
{
    internal static class RecurringJobExtensions
    {
        public static IDisposable AcquireDistributedRecurringJobLock(
            [NotNull] this IStorageConnection connection,
            [NotNull] string recurringJobId,
            TimeSpan timeout)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));

            return connection.AcquireDistributedLock($"lock:recurring-job:{recurringJobId}", timeout);
        }

        public static RecurringJobEntity GetRecurringJob(
            [NotNull] this IStorageConnection connection,
            [NotNull] string recurringJobId,
            [NotNull] ITimeZoneResolver timeZoneResolver,
            DateTime now)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (timeZoneResolver == null) throw new ArgumentNullException(nameof(timeZoneResolver));

            var recurringJob = connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
            if (recurringJob == null || recurringJob.Count == 0) return null;

            return new RecurringJobEntity(recurringJobId, recurringJob, timeZoneResolver, now);
        }

        public static RecurringJobEntity GetOrCreateRecurringJob(
            [NotNull] this IStorageConnection connection,
            [NotNull] string recurringJobId,
            [NotNull] ITimeZoneResolver timeZoneResolver,
            DateTime now)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (timeZoneResolver == null) throw new ArgumentNullException(nameof(timeZoneResolver));

            var recurringJob = connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
            if (recurringJob == null || recurringJob.Count == 0) recurringJob = new Dictionary<string, string>();

            return new RecurringJobEntity(recurringJobId, recurringJob, timeZoneResolver, now);
        }

        public static void UpdateRecurringJob(
            [NotNull] this IWriteOnlyTransaction transaction,
            [NotNull] string recurringJobId,
            [NotNull] IReadOnlyDictionary<string, string> changedFields,
            [CanBeNull] DateTime? nextExecution)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (changedFields == null) throw new ArgumentNullException(nameof(changedFields));

            if (changedFields.Count > 0)
            {
                transaction.SetRangeInHash($"recurring-job:{recurringJobId}", changedFields);
            }

            transaction.AddToSet(
                "recurring-jobs",
                recurringJobId,
                nextExecution.HasValue ? JobHelper.ToTimestamp(nextExecution.Value) : -1.0D);
        }

        public static BackgroundJob TriggerRecurringJob(
            [NotNull] this IBackgroundJobFactory factory,
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] IProfiler profiler,
            [NotNull] RecurringJobEntity recurringJob,
            DateTime now)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (profiler == null) throw new ArgumentNullException(nameof(profiler));
            if (recurringJob == null) throw new ArgumentNullException(nameof(recurringJob));
            
            var initialParams = SerializationHelper.Deserialize<IDictionary<string, object>>(recurringJob.InitialParams);
            
            var context = new CreateContext(storage, connection, recurringJob.Job, null, profiler, initialParams);
            context.Parameters["RecurringJobId"] = recurringJob.RecurringJobId;
            context.Parameters["Time"] = JobHelper.ToTimestamp(now);

            var backgroundJob = factory.Create(context);

            recurringJob.LastExecution = now;
            recurringJob.LastJobId = backgroundJob?.Id;

            return backgroundJob;
        }

        public static void EnqueueBackgroundJob(
            [NotNull] this IStateMachine stateMachine,
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] RecurringJobEntity recurringJob,
            [NotNull] BackgroundJob backgroundJob,
            [CanBeNull] string reason,
            [NotNull] IProfiler profiler)
        {
            if (stateMachine == null) throw new ArgumentNullException(nameof(stateMachine));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (recurringJob == null) throw new ArgumentNullException(nameof(recurringJob));
            if (backgroundJob == null) throw new ArgumentNullException(nameof(backgroundJob));
            if (profiler == null) throw new ArgumentNullException(nameof(profiler));

            var state = new EnqueuedState { Reason = reason };

            if (recurringJob.Queue != null)
            {
                state.Queue = recurringJob.Queue;
            }

            stateMachine.ApplyState(new ApplyStateContext(
                storage,
                connection,
                transaction,
                backgroundJob,
                state,
                null,
                profiler));
        }
    }
}