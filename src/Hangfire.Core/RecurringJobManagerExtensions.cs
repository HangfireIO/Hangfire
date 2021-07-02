// This file is part of Hangfire.
// Copyright © 2016 Sergey Odinokov.
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
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    public static class RecurringJobManagerExtensions
    {
        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Job job,
            [NotNull] string cronExpression)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            manager.AddOrUpdate(recurringJobId, job, cronExpression, new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });
        }

        [Obsolete("Please use the AddOrUpdate(string, Job, string, RecurringJobOptions) method instead. Will be removed in 2.0.0.")]
        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Job job,
            [NotNull] string cronExpression,
            [NotNull] TimeZoneInfo timeZone)
        {
            AddOrUpdate(manager, recurringJobId, job, cronExpression, timeZone, EnqueuedState.DefaultQueue);
        }

        [Obsolete("Please use the AddOrUpdate(string, Job, string, RecurringJobOptions) method instead. Will be removed in 2.0.0.")]
        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Job job,
            [NotNull] string cronExpression,
            [NotNull] TimeZoneInfo timeZone,
            [NotNull] string queue)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (timeZone == null) throw new ArgumentNullException(nameof(timeZone));
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            manager.AddOrUpdate(
                recurringJobId,
                job,
                cronExpression,
                new RecurringJobOptions { QueueName = queue, TimeZone = timeZone });
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Job job,
            [NotNull] string cronExpression,
            [NotNull] RecurringJobOptions options)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            manager.AddOrUpdate(recurringJobId, job, cronExpression, options);
        }

        [Obsolete("Please use the AddOrUpdate(string, Expression<Action>, Func<string>, RecurringJobOptions) extension method instead. Will be removed in 2.0.0.")]
        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action> methodCall,
            [NotNull] Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action> methodCall,
            [NotNull] Func<string> cronExpression)
        {
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression, new RecurringJobOptions());
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action> methodCall,
            [NotNull] Func<string> cronExpression,
            [NotNull] RecurringJobOptions options)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression(), options);
        }

        [Obsolete("Please use the AddOrUpdate<T>(string, Expression<Action<T>>, Func<string>, RecurringJobOptions) extension method instead. Will be removed in 2.0.0.")]
        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action<T>> methodCall,
            [NotNull] Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action<T>> methodCall,
            [NotNull] Func<string> cronExpression)
        {
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression, new RecurringJobOptions());
        }

        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action<T>> methodCall,
            [NotNull] Func<string> cronExpression,
            [NotNull] RecurringJobOptions options)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression(), options);
        }

        [Obsolete("Please use the AddOrUpdate(string, Expression<Action>, string, RecurringJobOptions) extension method instead. Will be removed in 2.0.0.")]
        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action> methodCall,
            [NotNull] string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));

            var job = Job.FromExpression(methodCall);
            manager.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action> methodCall,
            [NotNull] string cronExpression)
        {
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression, new RecurringJobOptions());
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action> methodCall,
            [NotNull] string cronExpression,
            [NotNull] RecurringJobOptions options)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var job = Job.FromExpression(methodCall);
            manager.AddOrUpdate(recurringJobId, job, cronExpression, options);
        }

        [Obsolete("Please use the AddOrUpdate<T>(string, Expression<Action<T>>, string, RecurringJobOptions) extension method instead. Will be removed in 2.0.0.")]
        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action<T>> methodCall,
            [NotNull] string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));

            var job = Job.FromExpression(methodCall);
            manager.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<T, Task>> methodCall,
            [NotNull] string cronExpression)
        {
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression, new RecurringJobOptions());
        }

        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action<T>> methodCall,
            [NotNull] string cronExpression,
            [NotNull] RecurringJobOptions options)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var job = Job.FromExpression(methodCall);
            manager.AddOrUpdate(recurringJobId, job, cronExpression, options);
        }

        [Obsolete("Please use the AddOrUpdate(string, Expression<Func<Task>>, Func<string>, RecurringJobOptions) extension method instead. Will be removed in 2.0.0.")]
        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<Task>> methodCall,
            [NotNull] Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<Task>> methodCall,
            [NotNull] Func<string> cronExpression)
        {
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression, new RecurringJobOptions());
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<Task>> methodCall,
            [NotNull] Func<string> cronExpression,
            [NotNull] RecurringJobOptions options)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression(), options);
        }

        [Obsolete("Please use the AddOrUpdate<T>(string, Expression<Func<T, Task>>, Func<string>, RecurringJobOptions) extension method instead. Will be removed in 2.0.0.")]
        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<T, Task>> methodCall,
            [NotNull] Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<T, Task>> methodCall,
            [NotNull] Func<string> cronExpression)
        {
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression, new RecurringJobOptions());
        }

        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<T, Task>> methodCall,
            [NotNull] Func<string> cronExpression,
            [NotNull] RecurringJobOptions options)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression(), options);
        }

        [Obsolete("Please use the AddOrUpdate(string, Expression<Func<Task>>, string, RecurringJobOptions) extension method instead. Will be removed in 2.0.0.")]
        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<Task>> methodCall,
            [NotNull] string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));

            var job = Job.FromExpression(methodCall);
            manager.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<Task>> methodCall,
            [NotNull] string cronExpression)
        {
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression, new RecurringJobOptions());
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<Task>> methodCall,
            [NotNull] string cronExpression,
            [NotNull] RecurringJobOptions options)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var job = Job.FromExpression(methodCall);
            manager.AddOrUpdate(recurringJobId, job, cronExpression, options);
        }

        [Obsolete("Please use the AddOrUpdate<T>(string, Expression<Func<T, Task>>, string, RecurringJobOptions) extension method instead. Will be removed in 2.0.0.")]
        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<T, Task>> methodCall,
            [NotNull] string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));

            var job = Job.FromExpression(methodCall);
            manager.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Action<T>> methodCall,
            [NotNull] string cronExpression)
        {
            AddOrUpdate(manager, recurringJobId, methodCall, cronExpression, new RecurringJobOptions());
        }

        public static void AddOrUpdate<T>(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Expression<Func<T, Task>> methodCall,
            [NotNull] string cronExpression,
            [NotNull] RecurringJobOptions options)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var job = Job.FromExpression(methodCall);
            manager.AddOrUpdate(recurringJobId, job, cronExpression, options);
        }
    }
}