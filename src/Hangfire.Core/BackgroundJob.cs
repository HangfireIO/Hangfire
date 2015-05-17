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
using Hangfire.Annotations;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Represents a static facade for the Hangfire Client API.
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
        public static string Enqueue([InstantHandle] Expression<Action> methodCall)
        {
            var client = ClientFactory();
            return client.Enqueue(methodCall);
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
        public static string Enqueue<T>([InstantHandle] Expression<Action<T>> methodCall)
        {
            var client = ClientFactory();
            return client.Enqueue(methodCall);
        }

        /// <summary>
        /// Creates a new background job based on a specified static method
        /// call expression and schedules it to be enqueued after a given delay.
        /// </summary>
        /// 
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule([InstantHandle] Expression<Action> methodCall, TimeSpan delay)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, delay);
        }

        /// <summary>
        /// Creates a new background job based on a specified method call expression
        /// and schedules it to be enqueued at the given moment of time.
        /// </summary>
        /// 
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">The moment of time at which the job will be enqueued.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string Schedule([InstantHandle] Expression<Action> methodCall, DateTimeOffset enqueueAt)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, enqueueAt);
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
        public static string Schedule<T>([InstantHandle] Expression<Action<T>> methodCall, TimeSpan delay)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, delay);
        }

        /// <summary>
        /// Creates a new background job based on a specified method call expression
        /// and schedules it to be enqueued at the given moment of time.
        /// </summary>
        /// 
        /// <typeparam name="T">The type whose method will be invoked during the job processing.</typeparam>
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">The moment of time at which the job will be enqueued.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string Schedule<T>([InstantHandle] Expression<Action<T>> methodCall, DateTimeOffset enqueueAt)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, enqueueAt);
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
            var client = ClientFactory();
            return client.Delete(jobId);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="DeletedState"/>. State change is being only performed
        /// if current job state is equal to the <paramref name="fromState"/> value.
        /// <seealso cref="BackgroundJobClientExtensions.Delete(IBackgroundJobClient, string, string)"/>
        /// </summary>
        /// 
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="fromState">Current state assertion, or null if unneeded.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Delete(string jobId, string fromState)
        {
            var client = ClientFactory();
            return client.Delete(jobId, fromState);
        }
        
        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="EnqueuedState"/>.
        /// </summary>
        /// 
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Requeue(string jobId)
        {
            var client = ClientFactory();
            return client.Requeue(jobId);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="EnqueuedState"/>. If <paramref name="fromState"/> value 
        /// is not null, state change will be performed only if the current state name 
        /// of a job equal to the given value.
        /// </summary>
        /// 
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="fromState">Current state assertion, or null if unneeded.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Requeue(string jobId, string fromState)
        {
            var client = ClientFactory();
            return client.Requeue(jobId, fromState);
        }

        public static string ContinueWith(string parentId, [InstantHandle] Expression<Action> methodCall)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall);
        }

        public static string ContinueWith<T>(string parentId, [InstantHandle] Expression<Action<T>> methodCall)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall);
        }

        public static string ContinueWith(string parentId, [InstantHandle] Expression<Action> methodCall, JobContinuationOptions options)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall, options);
        }

        public static string ContinueWith<T>(string parentId, [InstantHandle] Expression<Action<T>> methodCall, JobContinuationOptions options)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall, options);
        }
    }
}
