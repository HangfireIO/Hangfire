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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    public static class RecurringJob
    {
        private static readonly Lazy<RecurringJobManager> Instance = new Lazy<RecurringJobManager>(
            () => new RecurringJobManager(), LazyThreadSafetyMode.PublicationOnly);

        public static void AddOrUpdate(
            [NotNull, InstantHandle] Expression<Action> methodCall,
            [NotNull] Func<string> cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            [NotNull] Func<string> cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            [NotNull, InstantHandle] Expression<Action> methodCall,
            [NotNull] string cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            [NotNull] string cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate(
            [NotNull] string recurringJobId,
            [NotNull, InstantHandle] Expression<Action> methodCall,
            [NotNull] Func<string> cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull] string recurringJobId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            [NotNull] Func<string> cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            [NotNull] string recurringJobId,
            [NotNull, InstantHandle] Expression<Action> methodCall,
            [NotNull] string cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull] string recurringJobId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            [NotNull] string cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate(
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall,
            [NotNull] Func<string> cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            [NotNull] Func<string> cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall,
            [NotNull] string cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            [NotNull] string cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }
        
        public static void AddOrUpdate(
            [NotNull] string recurringJobId,
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall,
            [NotNull] Func<string> cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull] string recurringJobId,
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            [NotNull] Func<string> cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            [NotNull] string recurringJobId,
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall,
            [NotNull] string cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull] string recurringJobId,
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            [NotNull] string cronExpression,
            [CanBeNull] TimeZoneInfo timeZone = null,
            [NotNull] string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void RemoveIfExists([NotNull] string recurringJobId)
        {
            Instance.Value.RemoveIfExists(recurringJobId);
        }

        [Obsolete("Please use the TriggerJob method instead. Will be removed in 2.0.0.")]
        public static void Trigger([NotNull] string recurringJobId)
        {
            Instance.Value.Trigger(recurringJobId);
        }

        public static string TriggerJob([NotNull] string recurringJobId)
        {
            return Instance.Value.TriggerJob(recurringJobId);
        }

        private static string GetRecurringJobId(Job job)
        {
            return $"{job.Type.ToGenericTypeString()}.{job.Method.Name}";
        }
    }
}
