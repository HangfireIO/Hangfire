// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq.Expressions;
using HangFire.Common;

namespace HangFire
{
    public static class RecurringJob
    {
        private static readonly Lazy<RecurringJobManager> Instance = new Lazy<RecurringJobManager>(
            () => new RecurringJobManager(JobStorage.Current));

        public static void AddOrUpdate(Expression<Action> methodCall, string cronExpression)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression);
        }

        public static void AddOrUpdate(Expression<Action> methodCall, Func<string> cronExpression)
        {
            AddOrUpdate(methodCall, cronExpression());
        }

        public static void AddOrUpdate<T>(Expression<Action<T>> methodCall, string cronExpression)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression);
        }

        public static void AddOrUpdate<T>(Expression<Action<T>> methodCall, Func<string> cronExpression)
        {
            AddOrUpdate(methodCall, cronExpression());
        }

        private static string GetRecurringJobId(Job job)
        {
            return String.Format("{0}.{1}", job.Type.Name, job.Method.Name);
        }
    }
}
