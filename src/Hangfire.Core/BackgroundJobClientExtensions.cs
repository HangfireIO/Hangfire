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
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    public static class BackgroundJobClientExtensions
    {
        /// <summary>
        /// Creates a background job based on a specified static method 
        /// call expression and places it into its actual queue. 
        /// Please, see the <see cref="QueueAttribute"/> to learn how to 
        /// place the job on a non-default queue.
        /// </summary>
        /// 
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Static method call expression that will be marshalled to the Server.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue([NotNull] this IBackgroundJobClient client, [InstantHandle] Expression<Action> methodCall)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(methodCall, new EnqueuedState());
        }

        /// <summary>
        /// Creates a background job based on a specified instance method 
        /// call expression and places it into its actual queue. 
        /// Please, see the <see cref="QueueAttribute"/> to learn how to 
        /// place the job on a non-default queue.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue<T>([NotNull] this IBackgroundJobClient client, [InstantHandle] Expression<Action<T>> methodCall)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(methodCall, new EnqueuedState());
        }

        /// <summary>
        /// Creates a new background job based on a specified static method
        /// call expression and schedules it to be enqueued after a given delay.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule([NotNull] this IBackgroundJobClient client, [InstantHandle] Expression<Action> methodCall, TimeSpan delay)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(methodCall, new ScheduledState(delay));
        }

        /// <summary>
        /// Creates a new background job based on a specified method call expression
        /// and schedules it to be enqueued at the specified moment of time.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">Moment of time at which the job will be enqueued.</param>
        /// <returns>Unique identifier or a created job.</returns>
        public static string Schedule(
            [NotNull] this IBackgroundJobClient client,
            [InstantHandle] Expression<Action> methodCall,
            DateTimeOffset enqueueAt)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(methodCall, new ScheduledState(enqueueAt.UtcDateTime));
        }

        /// <summary>
        /// Creates a new background job based on a specified instance method
        /// call expression and schedules it to be enqueued after a given delay.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule<T>([NotNull] this IBackgroundJobClient client, [InstantHandle] Expression<Action<T>> methodCall, TimeSpan delay)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(methodCall, new ScheduledState(delay));
        }

        /// <summary>
        /// Creates a new background job based on a specified method expression and schedules
        /// it to be enqueued at the specified moment.
        /// </summary>
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">Moment at which the job will be enqueued.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string Schedule<T>(
            [NotNull] this IBackgroundJobClient client,
            [InstantHandle] Expression<Action<T>> methodCall,
            DateTimeOffset enqueueAt)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(methodCall, new ScheduledState(enqueueAt.UtcDateTime));
        }

        /// <summary>
        /// Creates a new background job based on a specified static method
        /// within a given state.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Static method call expression that will be marshalled to the Server.</param>
        /// <param name="state">Initial state of a job.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Create(
            [NotNull] this IBackgroundJobClient client,
            [InstantHandle] Expression<Action> methodCall,
            IState state)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Creates a new background job based on a specified instance method
        /// within a given state.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="state">Initial state of a job.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Create<T>(
            [NotNull] this IBackgroundJobClient client,
            [InstantHandle] Expression<Action<T>> methodCall,
            IState state)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Changes state of a job with the given <paramref name="jobId"/> to
        /// the specified one. 
        /// </summary>
        /// 
        /// <param name="client">An instance of <see cref="IBackgroundJobClient"/> implementation.</param>
        /// <param name="jobId">A job, whose state is being changed.</param>
        /// <param name="state">New state for a job.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool ChangeState([NotNull] this IBackgroundJobClient client, string jobId, IState state)
        {
            if (client == null) throw new ArgumentNullException("client");
            return client.ChangeState(jobId, state, null);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="DeletedState"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// The job is not actually being deleted, this method changes only
        /// its state.
        /// 
        /// This operation does not provides guarantee that the job will not be 
        /// performed. If you deleting a job that is performing right now, it 
        /// will be performed anyway, despite of this call.
        /// 
        /// The method returns result of a state transition. It can be false
        /// if a job was expired, its method does not exist or there was an
        /// exception during the state change process.
        /// </remarks>
        /// 
        /// <param name="client">An instance of <see cref="IBackgroundJobClient"/> implementation.</param>
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Delete([NotNull] this IBackgroundJobClient client, string jobId)
        {
            return Delete(client, jobId, null);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="DeletedState"/>. If <paramref name="fromState"/> value 
        /// is not null, state change will be performed only if the current state name 
        /// of a job equal to the given value.
        /// </summary>
        /// 
        /// <remarks>
        /// The job is not actually being deleted, this method changes only
        /// its state.
        /// 
        /// This operation does not provides guarantee that the job will not be 
        /// performed. If you deleting a job that is performing right now, it 
        /// will be performed anyway, despite of this call.
        /// 
        /// The method returns result of a state transition. It can be false
        /// if a job was expired, its method does not exist or there was an
        /// exception during the state change process.
        /// </remarks>
        /// 
        /// <param name="client">An instance of <see cref="IBackgroundJobClient"/> implementation.</param>
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="fromState">Current state assertion, or null if unneeded.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Delete([NotNull] this IBackgroundJobClient client, string jobId, string fromState)
        {
            if (client == null) throw new ArgumentNullException("client");

            var state = new DeletedState();
            return client.ChangeState(jobId, state, fromState);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="EnqueuedState"/>.
        /// </summary>
        /// 
        /// <param name="client">An instance of <see cref="IBackgroundJobClient"/> implementation.</param>
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Requeue([NotNull] this IBackgroundJobClient client, string jobId)
        {
            return Requeue(client, jobId, null);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="EnqueuedState"/>. If <paramref name="fromState"/> value 
        /// is not null, state change will be performed only if the current state name 
        /// of a job equal to the given value.
        /// </summary>
        /// 
        /// <param name="client">An instance of <see cref="IBackgroundJobClient"/> implementation.</param>
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="fromState">Current state assertion, or null if unneeded.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Requeue([NotNull] this IBackgroundJobClient client, string jobId, string fromState)
        {
            if (client == null) throw new ArgumentNullException("client");

            var state = new EnqueuedState();
            return client.ChangeState(jobId, state, fromState);
        }

        public static string ContinueWith(this IBackgroundJobClient client, string parentId,
            [InstantHandle] Expression<Action> methodCall)
        {
            return ContinueWith(client, parentId, methodCall, new EnqueuedState());
        }

        public static string ContinueWith(
            this IBackgroundJobClient client,
            string parentId,
            [InstantHandle] Expression<Action> methodCall,
            IState nextState)
        {
            return ContinueWith(client, parentId, methodCall, nextState, JobContinuationOptions.OnlyOnSucceededState);
        }

        public static string ContinueWith(
            [NotNull] this IBackgroundJobClient client,
            string parentId,
            [InstantHandle] Expression<Action> methodCall,
            JobContinuationOptions options)
        {
            return ContinueWith(client, parentId, methodCall, new EnqueuedState(), options);
        }

        public static string ContinueWith(
            [NotNull] this IBackgroundJobClient client,
            string parentId,
            [InstantHandle] Expression<Action> methodCall,
            IState nextState,
            JobContinuationOptions options)
        {
            if (client == null) throw new ArgumentNullException("client");

            var state = new AwaitingState(parentId, nextState, options);
            return client.Create(Job.FromExpression(methodCall), state);
        }
    }
}
