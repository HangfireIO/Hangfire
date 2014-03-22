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
using HangFire.Client;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire
{
    /// <summary>
    /// Represents a facade for the HangFire Client API.
    /// </summary>
    public abstract class BackgroundJob
    {
        [Obsolete("Background job definitions that are based on the BackgroundJob class are no longer supported. Please, see 'Upgrading' section of the documentation.")]
        public abstract void Perform();

        private static Func<IJobClient> _clientFactory =
            () => new JobClient(JobStorage.Current.GetConnection());
        private static readonly object ClientFactoryLock = new object();

        public static Func<IJobClient> ClientFactory
        {
            get
            {
                lock (ClientFactoryLock)
                {
                    return _clientFactory;
                }
            }
            set
            {
                lock (ClientFactoryLock)
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException();
                    }

                    _clientFactory = value;
                }
            }
        }

        /// <summary>
        /// Creates a background job based on a specified static method 
        /// call experession and places it into its actual queue. 
        /// Please, see the <see cref="QueueAttribute"/> to learn how to 
        /// place the job on a non-default queue.
        /// </summary>
        /// 
        /// <param name="methodCall">Static method call expression that will be marshalled to the Server.</param>
        /// <returns>Unique identifier of the created job.</returns>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="methodCall"/> does not contain <see cref="MethodCallExpression"/>.
        /// </exception>
        /// <exception cref="CreateJobFailedException">Job creation has failed.</exception>
        public static string Enqueue(Expression<Action> methodCall)
        {
            return CreateInternal(
                Job.FromExpression(methodCall), CreateEnqueuedState());
        }

        /// <summary>
        /// Creates a background job based on a specified instance method 
        /// call experession and places it into its actual queue. 
        /// Please, see the <see cref="QueueAttribute"/> to learn how to 
        /// place the job on a non-default queue.
        /// </summary>
        /// 
        /// <typeparam name="TJob">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <returns>Unique identifier of the created job.</returns>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="methodCall"/> does not contain <see cref="MethodCallExpression"/>.
        /// </exception>
        /// <exception cref="CreateJobFailedException">Job creation has failed.</exception>
        public static string Enqueue<TJob>(Expression<Action<TJob>> methodCall)
        {
            return CreateInternal(
                Job.FromExpression(methodCall), CreateEnqueuedState());
        }

        /// <summary>
        /// Creates a new background job based on a specified static method
        /// call expression and schedules it to be enqueued after a given delay.
        /// </summary>
        /// 
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="methodCall"/> does not contain <see cref="MethodCallExpression"/>.
        /// </exception>
        /// <exception cref="CreateJobFailedException">Job creation has failed.</exception>
        public static string Schedule(Expression<Action> methodCall, TimeSpan delay)
        {
            return CreateInternal(
                Job.FromExpression(methodCall), CreateScheduledState(delay));
        }

        /// <summary>
        /// Creates a new background job based on a specified instance method
        /// call expression and schedules it to be enqueued after a given delay.
        /// </summary>
        /// 
        /// <typeparam name="TJob">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="methodCall"/> does not contain <see cref="MethodCallExpression"/>.
        /// </exception>
        /// <exception cref="CreateJobFailedException">Job creation has failed.</exception>
        public static string Schedule<TJob>(Expression<Action<TJob>> methodCall, TimeSpan delay)
        {
            return CreateInternal(
                Job.FromExpression(methodCall), CreateScheduledState(delay));
        }

        private static State CreateEnqueuedState()
        {
            return new EnqueuedState();
        }

        private static State CreateScheduledState(TimeSpan delay)
        {
            return CreateScheduledState(DateTime.UtcNow.Add(delay));
        }

        private static State CreateScheduledState(DateTime utcDateTime)
        {
            return new ScheduledState(utcDateTime);
        }

        private static string CreateInternal(Job job, State state)
        {
            using (var client = ClientFactory())
            {
                return client.CreateJob(job, state);
            }
        }
    }
}
