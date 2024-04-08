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
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
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
            [NotNull] string recurringJobId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));

            var recurringJob = connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
            if (recurringJob == null || recurringJob.Count == 0) return null;

            return new RecurringJobEntity(recurringJobId, recurringJob);
        }

        public static RecurringJobEntity GetOrCreateRecurringJob(
            [NotNull] this IStorageConnection connection,
            [NotNull] string recurringJobId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));

            var recurringJob = connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
            if (recurringJob == null || recurringJob.Count == 0) recurringJob = new Dictionary<string, string>();

            return new RecurringJobEntity(recurringJobId, recurringJob);
        }

        public static void UpdateRecurringJob(
            [NotNull] this IWriteOnlyTransaction transaction,
            [NotNull] RecurringJobEntity recurringJob,
            [NotNull] IReadOnlyDictionary<string, string> changedFields,
            [CanBeNull] ILog logger)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (recurringJob == null) throw new ArgumentNullException(nameof(recurringJob));
            if (changedFields == null) throw new ArgumentNullException(nameof(changedFields));

            if (changedFields.Count > 0)
            {
                transaction.SetRangeInHash($"recurring-job:{recurringJob.RecurringJobId}", changedFields);
            }

            var score = recurringJob.NextExecution.HasValue ? JobHelper.ToTimestamp(recurringJob.NextExecution.Value) : -1.0D;

            if (logger != null && logger.IsTraceEnabled())
            {
                logger.Trace($"Recurring job '{recurringJob.RecurringJobId}' is being updated. RecurringJob: ({recurringJob}), Changes: ({String.Join(";", changedFields.Select(x => $"{x.Key}:{x.Value}"))})");
            }

            transaction.AddToSet(
                "recurring-jobs",
                recurringJob.RecurringJobId,
                score);
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

            if (recurringJob.Job == null)
            {
                throw new InvalidOperationException("The 'Job' field has a null or empty value");
            }

            var job = InvocationData.DeserializePayload(recurringJob.Job).DeserializeJob();

            var context = new CreateContext(storage, connection, job, null, profiler);
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