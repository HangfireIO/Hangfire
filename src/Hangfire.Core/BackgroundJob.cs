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
using Hangfire.Annotations;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Provides static methods for creating <i>fire-and-forget</i>, <i>delayed</i>
    /// jobs and <i>continuations</i> as well as re-queue and delete existing
    /// background jobs.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>This class is a wrapper for the <see cref="IBackgroundJobClient"/> 
    /// interface and its default implementation, <see cref="BackgroundJobClient"/>
    /// class, that was created for the most simple scenarios. Please consider 
    /// using the types above in real world applications.</para>
    /// <para>This class also contains undocumented constructor and instance 
    /// members. They are hidden to not to confuse new users. You can freely 
    /// use them in low-level API.</para>
    /// </remarks>
    /// 
    /// <seealso cref="IBackgroundJobClient"/>
    /// <seealso cref="BackgroundJobClient"/>
    /// 
    /// <threadsafety static="true" instance="false" />
    public partial class BackgroundJob
    {
        private static readonly Lazy<IBackgroundJobClient> CachedClient 
            = new Lazy<IBackgroundJobClient>(() => new BackgroundJobClient()); 

        private static readonly Func<IBackgroundJobClient> DefaultFactory
            = () => CachedClient.Value;

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
        /// Creates a new fire-and-forget job based on a given method call expression.
        /// </summary>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a background job.</returns>
        /// 
        /// <exception cref="ArgumentNullException">
        /// <paramref name="methodCall"/> is <see langword="null"/>.
        /// </exception>
        /// 
        /// <seealso cref="EnqueuedState"/>
        /// <seealso cref="O:Hangfire.IBackgroundJobClient.Enqueue"/>
        public static string Enqueue([NotNull, InstantHandle] Expression<Action> methodCall)
        {
            var client = ClientFactory();
            return client.Enqueue(methodCall);
        }

        /// <summary>
        /// Creates a new fire-and-forget job based on a given method call expression.
        /// </summary>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a background job.</returns>
        /// 
        /// <exception cref="ArgumentNullException">
        /// <paramref name="methodCall"/> is <see langword="null"/>.
        /// </exception>
        /// 
        /// <seealso cref="EnqueuedState"/>
        /// <seealso cref="O:Hangfire.IBackgroundJobClient.Enqueue"/>
        public static string Enqueue<T>([NotNull, InstantHandle] Expression<Action<T>> methodCall)
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
        public static string Schedule(
            [NotNull, InstantHandle] Expression<Action> methodCall, 
            TimeSpan delay)
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
        public static string Schedule(
            [NotNull, InstantHandle] Expression<Action> methodCall, 
            DateTimeOffset enqueueAt)
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
        public static string Schedule<T>(
            [NotNull, InstantHandle] Expression<Action<T>> methodCall, 
            TimeSpan delay)
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
        public static string Schedule<T>(
            [NotNull, InstantHandle] Expression<Action<T>> methodCall, 
            DateTimeOffset enqueueAt)
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
        public static bool Delete([NotNull] string jobId)
        {
            var client = ClientFactory();
            return client.Delete(jobId);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="DeletedState"/>. State change is only performed
        /// if current job state is equal to the <paramref name="fromState"/> value.
        /// <seealso cref="BackgroundJobClientExtensions.Delete(IBackgroundJobClient, string, string)"/>
        /// </summary>
        /// 
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="fromState">Current state assertion, or null if unneeded.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Delete([NotNull] string jobId, [CanBeNull] string fromState)
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
        public static bool Requeue([NotNull] string jobId)
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
        public static bool Requeue([NotNull] string jobId, [CanBeNull] string fromState)
        {
            var client = ClientFactory();
            return client.Requeue(jobId, fromState);
        }

        public static string ContinueWith(
            [NotNull] string parentId, 
            [NotNull, InstantHandle] Expression<Action> methodCall)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall);
        }

        public static string ContinueWith<T>(
            [NotNull] string parentId, 
            [NotNull, InstantHandle] Expression<Action<T>> methodCall)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall);
        }

        public static string ContinueWith(
            [NotNull] string parentId, 
            [NotNull, InstantHandle] Expression<Action> methodCall, 
            JobContinuationOptions options)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall, options);
        }

        public static string ContinueWith<T>(
            [NotNull] string parentId, 
            [NotNull, InstantHandle] Expression<Action<T>> methodCall, 
            JobContinuationOptions options)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall, options);
        }
    }
}
