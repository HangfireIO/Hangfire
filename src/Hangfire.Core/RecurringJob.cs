﻿// This file is part of Hangfire.
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
using Hangfire.Common;

namespace Hangfire
{
    public static class RecurringJob
    {
        private static readonly Lazy<RecurringJobManager> Instance = new Lazy<RecurringJobManager>(
            () => new RecurringJobManager());

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Action> methodCall,
            string cronExpression)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression);
        }

        public static void AddOrUpdate(Expression<Action> methodCall, string cronExpression)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression);
        }

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Action> methodCall,
            Func<string> cronExpression)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression());
        }

        public static void AddOrUpdate(Expression<Action> methodCall, Func<string> cronExpression)
        {
            AddOrUpdate(methodCall, cronExpression());
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Action<T>> methodCall,
            string cronExpression)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression);
        }

        public static void AddOrUpdate<T>(Expression<Action<T>> methodCall, string cronExpression)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Action<T>> methodCall,
            Func<string> cronExpression)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression());
        }

        public static void AddOrUpdate<T>(Expression<Action<T>> methodCall, Func<string> cronExpression)
        {
            AddOrUpdate(methodCall, cronExpression());
        }

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Action> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone);
        }

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Action> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Action<T>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Action<T>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone);
        }

        public static void AddOrUpdate(
            Expression<Action> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone);
        }

        public static void AddOrUpdate(
            Expression<Action> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone);
        }

        public static void AddOrUpdate<T>(
            Expression<Action<T>> methodCall, 
            Func<string> cronExpression, 
            TimeZoneInfo timeZone)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone);
        }

        public static void AddOrUpdate<T>(
            Expression<Action<T>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone);
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
            return job.ToString();
        }
    }
}
