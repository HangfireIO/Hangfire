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
using HangFire.States;

namespace HangFire
{
    /// <summary>
    /// Represents a static facade for the HangFire Client API.
    /// </summary>
    public abstract class BackgroundJob
    {
        private static readonly Func<IBackgroundJobClient> DefaultFactory
            = () => new BackgroundJobClient(JobStorage.Current);

        private static Func<IBackgroundJobClient> _clientFactory;
        private static readonly object ClientFactoryLock = new object();

        internal static Func<IBackgroundJobClient> ClientFactory
        {
            get
            {
                lock (ClientFactoryLock)
                {
                    return _clientFactory ?? DefaultFactory;
                }
            }
            set
            {
                lock (ClientFactoryLock)
                {
                    _clientFactory = value;
                }
            }
        }

        /// <summary>
        /// Creates a background job based on a specified static method 
        /// call expression and places it into its actual queue. 
        /// Please, see the <see cref="QueueAttribute"/> to learn how to 
        /// place the job on a non-default queue.
        /// </summary>
        /// 
        /// <param name="methodCall">Static method call expression that will be marshalled to the Server.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue(Expression<Action> methodCall)
        {
            using (var client = ClientFactory())
            {
                return client.Enqueue(methodCall);
            }
        }

        /// <summary>
        /// Creates a background job based on a specified instance method 
        /// call expression and places it into its actual queue. 
        /// Please, see the <see cref="QueueAttribute"/> to learn how to 
        /// place the job on a non-default queue.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue<T>(Expression<Action<T>> methodCall)
        {
            using (var client = ClientFactory())
            {
                return client.Enqueue(methodCall);
            }
        }

        /// <summary>
        /// Creates a background job based on a specified static method 
        /// call expression and places it into specified queue. 
        /// Please, note that the <see cref="QueueAttribute"/> can
        /// override the specified queue.
        /// </summary>
        /// 
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="queue">Queue to which the job will be placed.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue(Expression<Action> methodCall, string queue)
        {
            using (var client = ClientFactory())
            {
                return client.Enqueue(methodCall, queue);
            }
        }

        /// <summary>
        /// Creates a background job based on a specified instance method 
        /// call expression and places it into specified queue. 
        /// Please, note that the <see cref="QueueAttribute"/> can
        /// override the specified queue.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="queue">Queue to which the job will be placed.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue<T>(Expression<Action<T>> methodCall, string queue)
        {
            using (var client = ClientFactory())
            {
                return client.Enqueue(methodCall, queue);
            }
        }

        /// <summary>
        /// Creates a new background job based on a specified static method
        /// call expression and schedules it to be enqueued after a given delay.
        /// </summary>
        /// 
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule(Expression<Action> methodCall, TimeSpan delay)
        {
            using (var client = ClientFactory())
            {
                return client.Schedule(methodCall, delay);
            }
        }

        /// <summary>
        /// Creates a new background job based on a specified instance method
        /// call expression and schedules it to be enqueued after a given delay.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
        {
            using (var client = ClientFactory())
            {
                return client.Schedule(methodCall, delay);
            }
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="DeletedState"/>. 
        /// <seealso cref="BackgroundJobClientExtensions.Delete(IBackgroundJobClient, string)"/>
        /// </summary>
        /// 
        /// <param name="jobId">An identifier, that will be used to find a job.</param>
        /// <returns>True on a successfull state transition, false otherwise.</returns>
        public static bool Delete(string jobId)
        {
            using (var client = ClientFactory())
            {
                return client.Delete(jobId);
            }
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="DeletedState"/>. State change is being only performed
        /// if current job state is equal to the <paramref name="fromState"/> value.
        /// <seealso cref="BackgroundJobClientExtensions.Delete(IBackgroundJobClient, string, string)"/>
        /// </summary>
        /// 
        /// <param name="jobId">An identifier, that will be used to find a job.</param>
        /// <param name="fromState">Chang</param>
        /// <returns>True on a successfull state transition, false otherwise.</returns>
        public static bool Delete(string jobId, string fromState)
        {
            using (var client = ClientFactory())
            {
                return client.Delete(jobId, fromState);
            }
        }
    }
}
