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
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    public static class RecurringJob
    {
        private static readonly Lazy<RecurringJobManager> Instance = new Lazy<RecurringJobManager>(
            () => new RecurringJobManager());

        public static void AddOrUpdate(
            Expression<Action> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            Expression<Action<T>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        /// <summary>Creates or updates a described recurring <see cref="Job" /></summary>
        /// <param name="methodCall"><see cref="MethodInfo" /> to invoke</param>
        /// <param name="explicitType"><see cref="Type" /> to instantiate prior to invocation</param>
        /// <param name="parameters">Parameters to pass into <paramref name="methodCall" /> when invoking it. If no parameters, must be an empty array.</param>
        /// <param name="cronExpression"><see cref="Func{String}" /> yielding a CRON expression for <see cref="Job" /> execution frequency</param>
        /// <param name="timeZone"><see cref="TimeZoneInfo" /> describing the target time zone</param>
        /// <param name="queue">Name of the queue to use for processing the <see cref="Job" /></param>
        public static void AddOrUpdate(
            MethodInfo methodCall,
            Type explicitType,
            Object[] parameters,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(methodCall, explicitType, parameters, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            Expression<Action> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            Expression<Action<T>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        /// <summary>Creates or updates a described recurring <see cref="Job" /></summary>
        /// <param name="methodCall"><see cref="MethodInfo" /> to invoke</param>
        /// <param name="explicitType"><see cref="Type" /> to instantiate prior to invocation</param>
        /// <param name="parameters">Parameters to pass into <paramref name="methodCall" /> when invoking it. If no parameters, must be an empty array.</param>
        /// <param name="cronExpression">CRON expression for <see cref="Job" /> execution frequency</param>
        /// <param name="timeZone"><see cref="TimeZoneInfo" /> describing the target time zone</param>
        /// <param name="queue">Name of the queue to use for processing the <see cref="Job" /></param>
        public static void AddOrUpdate(
            MethodInfo methodCall,
            Type explicitType,
            Object[] parameters,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromReflection(methodCall, explicitType, parameters);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Action> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Action<T>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        /// <summary>Creates or updates a described recurring <see cref="Job" /></summary>
        /// <param name="recurringJobId">Unique ID used to identify the recurring <see cref="Job" /></param>
        /// <param name="methodCall"><see cref="MethodInfo" /> to invoke</param>
        /// <param name="explicitType"><see cref="Type" /> to instantiate prior to invocation</param>
        /// <param name="parameters">Parameters to pass into <paramref name="methodCall" /> when invoking it. If no parameters, must be an empty array.</param>
        /// <param name="cronExpression"><see cref="Func{String}" /> yielding a CRON expression for <see cref="Job" /> execution frequency</param>
        /// <param name="timeZone"><see cref="TimeZoneInfo" /> describing the target time zone</param>
        /// <param name="queue">Name of the queue to use for processing the <see cref="Job" /></param>
        public static void AddOrUpdate(
            string recurringJobId,
            MethodInfo methodCall,
            Type explicitType,
            Object[] parameters,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(recurringJobId, methodCall, explicitType, parameters, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Action> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Action<T>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        /// <summary>Creates or updates a described recurring <see cref="Job" /></summary>
        /// <param name="recurringJobId">Unique ID used to identify the recurring <see cref="Job" /></param>
        /// <param name="methodCall"><see cref="MethodInfo" /> to invoke</param>
        /// <param name="explicitType"><see cref="Type" /> to instantiate prior to invocation</param>
        /// <param name="parameters">Parameters to pass into <paramref name="methodCall" /> when invoking it. If no parameters, must be an empty array.</param>
        /// <param name="cronExpression">CRON expression for <see cref="Job" /> execution frequency</param>
        /// <param name="timeZone"><see cref="TimeZoneInfo" /> describing the target time zone</param>
        /// <param name="queue">Name of the queue to use for processing the <see cref="Job" /></param>
        public static void AddOrUpdate(
            string recurringJobId,
            MethodInfo methodCall,
            Type explicitType,
            Object[] parameters,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromReflection(methodCall, explicitType, parameters);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate(
            Expression<Func<Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            Expression<Func<T, Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            Expression<Func<Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            Expression<Func<T, Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Func<Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Func<T, Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Func<Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Func<T, Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void RemoveIfExists(string recurringJobId)
        {
            Instance.Value.RemoveIfExists(recurringJobId);
        }

        public static void Trigger(string recurringJobId)
        {
            Instance.Value.Trigger(recurringJobId);
        }

        private static string GetRecurringJobId(Job job)
        {
            return $"{job.Type.ToGenericTypeString()}.{job.Method.Name}";
        }
    }
}
