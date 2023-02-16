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
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Provides extension methods for the <see cref="IBackgroundJobClient"/>
    /// interface to simplify the creation of fire-and-forget jobs, delayed 
    /// jobs, continuations and other background jobs in well-known states.
    /// Also allows to re-queue and delete existing background jobs.
    /// </summary>
    public static class BackgroundJobClientExtensions
    {
        /// <summary>
        /// Creates a background job based on a specified lambda expression 
        /// and places it into its actual queue. 
        /// Please, see the <see cref="QueueAttribute"/> to learn how to 
        /// place the job on a non-default queue.
        /// </summary>
        /// 
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Static method call expression that will be marshalled to the Server.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue(
            [NotNull] this IBackgroundJobClient client, 
            [NotNull, InstantHandle] Expression<Action> methodCall)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new EnqueuedState());
        }

        /// <summary>
        /// Creates a background job based on a specified lambda expression 
        /// and places it into its actual queue. 
        /// Please, see the <see cref="QueueAttribute"/> to learn how to 
        /// place the job on a non-default queue.
        /// </summary>
        /// 
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Static method call expression that will be marshalled to the Server.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new EnqueuedState());
        }

        /// <summary>
        /// Creates a background job based on a specified lambda expression 
        /// and places it into its actual queue. 
        /// Please, see the <see cref="QueueAttribute"/> to learn how to 
        /// place the job on a non-default queue.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue<T>(
            [NotNull] this IBackgroundJobClient client, 
            [NotNull, InstantHandle] Expression<Action<T>> methodCall)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new EnqueuedState());
        }

        /// <summary>
        /// Creates a background job based on a specified lambda expression 
        /// and places it into its actual queue. 
        /// Please, see the <see cref="QueueAttribute"/> to learn how to 
        /// place the job on a non-default queue.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new EnqueuedState());
        }

        /// <summary>
        /// Creates a new background job based on a specified lambda expression 
        /// and schedules it to be enqueued after a given delay.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule(
            [NotNull] this IBackgroundJobClient client, 
            [NotNull, InstantHandle] Expression<Action> methodCall, 
            TimeSpan delay)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new ScheduledState(delay));
        }

        /// <summary>
        /// Creates a new background job based on a specified lambda expression 
        /// and schedules it to be enqueued after a given delay.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall,
            TimeSpan delay)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new ScheduledState(delay));
        }

        /// <summary>
        /// Creates a new background job based on a specified lambda expression
        /// and schedules it to be enqueued at the specified moment of time.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">Moment of time at which the job will be enqueued.</param>
        /// <returns>Unique identifier or a created job.</returns>
        public static string Schedule(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Action> methodCall,
            DateTimeOffset enqueueAt)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new ScheduledState(enqueueAt.UtcDateTime));
        }

        /// <summary>
        /// Creates a new background job based on a specified lambda expression
        /// and schedules it to be enqueued at the specified moment of time.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">Moment of time at which the job will be enqueued.</param>
        /// <returns>Unique identifier or a created job.</returns>
        public static string Schedule(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall,
            DateTimeOffset enqueueAt)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

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
        public static string Schedule<T>(
            [NotNull] this IBackgroundJobClient client, 
            [NotNull, InstantHandle] Expression<Action<T>> methodCall, 
            TimeSpan delay)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new ScheduledState(delay));
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
        public static string Schedule<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            TimeSpan delay)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new ScheduledState(delay));
        }

        /// <summary>
        /// Creates a new background job based on a specified lambda expression and schedules
        /// it to be enqueued at the specified moment.
        /// </summary>
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">Moment at which the job will be enqueued.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string Schedule<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            DateTimeOffset enqueueAt)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new ScheduledState(enqueueAt.UtcDateTime));
        }

        /// <summary>
        /// Creates a new background job based on a specified lambda expression and schedules
        /// it to be enqueued at the specified moment.
        /// </summary>
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">Moment at which the job will be enqueued.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string Schedule<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            DateTimeOffset enqueueAt)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(methodCall, new ScheduledState(enqueueAt.UtcDateTime));
        }

        /// <summary>
        /// Creates a new background job based on a specified lambda expression in a given state.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Static method call expression that will be marshalled to the Server.</param>
        /// <param name="state">Initial state of a job.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Create(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Action> methodCall,
            [NotNull] IState state)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Creates a new background job based on a specified lambda expression in a given state.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Static method call expression that will be marshalled to the Server.</param>
        /// <param name="state">Initial state of a job.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Create(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall,
            [NotNull] IState state)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Creates a new background job based on a specified instance method in a given state.
        /// </summary> 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="state">Initial state of a job.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Create<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            [NotNull] IState state)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Creates a new background job based on a specified instance method in a given state.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="state">Initial state of a job.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Create<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            [NotNull] IState state)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

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
        public static bool ChangeState(
            [NotNull] this IBackgroundJobClient client, 
            [NotNull] string jobId, 
            [NotNull] IState state)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
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
        /// This operation does not provide guarantee that the job will not be 
        /// performed. If you are deleting a job that is performing right now, it 
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
        public static bool Delete([NotNull] this IBackgroundJobClient client, [NotNull] string jobId)
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
        /// This operation does not provide guarantee that the job will not be 
        /// performed. If you are deleting a job that is performing right now, it 
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
        public static bool Delete(
            [NotNull] this IBackgroundJobClient client, 
            [NotNull] string jobId, 
            [CanBeNull] string fromState)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

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
        public static bool Requeue([NotNull] this IBackgroundJobClient client, [NotNull] string jobId)
        {
            return Requeue(client, jobId, null);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="ScheduledState"/>. If <paramref name="fromState"/> value
        /// is not null, state change will be performed only if the current state name
        /// of a job equal to the given value.
        /// </summary>
        ///
        /// <param name="client">An instance of <see cref="IBackgroundJobClient"/> implementation.</param>
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="delay">Delay, after which the job will be rescheduled.</param>
        /// <param name="fromState">Current state assertion, or null if unneeded.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Reschedule(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string jobId,
            TimeSpan delay,
            [CanBeNull] string fromState)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new ScheduledState(delay);
            return client.ChangeState(jobId, state, fromState);
        }

        /// <summary>
        ///   Changes state of a job with the specified <paramref name="jobId" />
        ///   to the <see cref="ScheduledState" />.
        /// </summary>
        /// 
        /// <param name="client">An instance of <see cref="IBackgroundJobClient" /> implementation.</param>
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="delay">Delay, after which the job will be rescheduled.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Reschedule([NotNull] this IBackgroundJobClient client, [NotNull] string jobId, TimeSpan delay)
        {
            return Reschedule(client, jobId, delay, null);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="ScheduledState"/>. If <paramref name="fromState"/> value
        /// is not null, state change will be performed only if the current state name
        /// of a job equal to the given value.
        /// </summary>
        ///
        /// <param name="client">An instance of <see cref="IBackgroundJobClient"/> implementation.</param>
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="enqueueAt">Moment of time at which the job will be rescheduled.</param>
        /// <param name="fromState">Current state assertion, or null if unneeded.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Reschedule(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string jobId,
            DateTimeOffset enqueueAt,
            [CanBeNull] string fromState)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new ScheduledState(enqueueAt.UtcDateTime);
            return client.ChangeState(jobId, state, fromState);
        }

        /// <summary>
        ///   Changes state of a job with the specified <paramref name="jobId" />
        ///   to the <see cref="ScheduledState" />.
        /// </summary>
        /// 
        /// <param name="client">An instance of <see cref="IBackgroundJobClient" /> implementation.</param>
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="enqueueAt">Moment of time at which the job will be rescheduled.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Reschedule([NotNull] this IBackgroundJobClient client, [NotNull] string jobId, DateTimeOffset enqueueAt)
        {
            return Reschedule(client, jobId, enqueueAt, null);
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
        public static bool Requeue(
            [NotNull] this IBackgroundJobClient client, 
            [NotNull] string jobId, 
            [CanBeNull] string fromState)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new EnqueuedState();
            return client.ChangeState(jobId, state, fromState);
        }

        /// <summary>
        /// Creates a new background job that will wait for a successful completion 
        /// of another background job to be triggered in the <see cref="EnqueuedState"/>.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a created job.</returns>
        [Obsolete("Deprecated for clarity, please use ContinueJobWith method with the same arguments. Will be removed in 2.0.0.")]
        public static string ContinueWith(
            [NotNull] this IBackgroundJobClient client, 
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action> methodCall)
        {
            return ContinueJobWith(client, parentId, methodCall);
        }

        /// <summary>
        /// Creates a new background job that will wait for a successful completion 
        /// of another background job to be triggered in the <see cref="EnqueuedState"/>.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueJobWith(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action> methodCall)
        {
            return ContinueJobWith(client, parentId, methodCall, new EnqueuedState());
        }

        /// <summary>
        /// Creates a new background job that will wait for a successful completion 
        /// of another background job to be triggered in the <see cref="EnqueuedState"/>.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a created job.</returns>
        [Obsolete("Deprecated for clarity, please use ContinueJobWith method with the same arguments. Will be removed in 2.0.0.")]
        public static string ContinueWith<T>(
            [NotNull] this IBackgroundJobClient client, 
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall)
        {
            return ContinueJobWith(client, parentId, methodCall);
        }

        /// <summary>
        /// Creates a new background job that will wait for a successful completion 
        /// of another background job to be triggered in the <see cref="EnqueuedState"/>.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueJobWith<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall)
        {
            return ContinueJobWith(client, parentId, methodCall, new EnqueuedState());
        }

        /// <summary>
        /// Creates a new background job that will wait for a successful completion 
        /// of another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered. 
        /// If null, then <see cref="EnqueuedState"/> is used.</param>
        /// <returns>Unique identifier of a created job.</returns>
        [Obsolete("Deprecated for clarity, please use ContinueJobWith method with the same arguments. Will be removed in 2.0.0.")]
        public static string ContinueWith(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action> methodCall,
            [NotNull] IState nextState)
        {
            return ContinueJobWith(client, parentId, methodCall, nextState);
        }

        /// <summary>
        /// Creates a new background job that will wait for a successful completion 
        /// of another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered. 
        /// If null, then <see cref="EnqueuedState"/> is used.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueJobWith(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action> methodCall,
            [NotNull] IState nextState)
        {
            return ContinueJobWith(client, parentId, methodCall, nextState, JobContinuationOptions.OnlyOnSucceededState);
        }

        /// <summary>
        /// Creates a new background job that will wait for a successful completion
        /// of another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered. 
        /// If null, then <see cref="EnqueuedState"/> is used.</param>
        /// <returns>Unique identifier of a created job.</returns>
        [Obsolete("Deprecated for clarity, please use ContinueJobWith method with the same arguments. Will be removed in 2.0.0.")]
        public static string ContinueWith<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            [NotNull] IState nextState)
        {
            return ContinueJobWith(client, parentId, methodCall, nextState);
        }

        /// <summary>
        /// Creates a new background job that will wait for a successful completion
        /// of another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered. 
        /// If null, then <see cref="EnqueuedState"/> is used.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueJobWith<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            [NotNull] IState nextState)
        {
            return ContinueJobWith(client, parentId, methodCall, nextState, JobContinuationOptions.OnlyOnSucceededState);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered
        /// in the <see cref="EnqueuedState"/>.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="options">Continuation options.</param>
        /// <returns>Unique identifier of a created job.</returns>
        [Obsolete("Deprecated for clarity, please use ContinueJobWith method with the same arguments. Will be removed in 2.0.0.")]
        public static string ContinueWith(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action> methodCall,
            JobContinuationOptions options)
        {
            return ContinueJobWith(client, parentId, methodCall, options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered
        /// in the <see cref="EnqueuedState"/>.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="options">Continuation options.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueJobWith(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action> methodCall,
            JobContinuationOptions options)
        {
            return ContinueJobWith(client, parentId, methodCall, new EnqueuedState(), options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered
        /// in the <see cref="EnqueuedState"/>.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param> 
        /// <param name="options">Continuation options.</param>
        /// <returns>Unique identifier of a created job.</returns>
        [Obsolete("Deprecated for clarity, please use ContinueJobWith method with the same arguments. Will be removed in 2.0.0.")]
        public static string ContinueWith<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            JobContinuationOptions options)
        {
            return ContinueJobWith(client, parentId, methodCall, options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered
        /// in the <see cref="EnqueuedState"/>.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param> 
        /// <param name="options">Continuation options.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueJobWith<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            JobContinuationOptions options)
        {
            return ContinueJobWith(client, parentId, methodCall, new EnqueuedState(), options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered.</param>
        /// <param name="options">Continuation options.</param>
        /// <returns>Unique identifier of a created job.</returns>
        [Obsolete("Deprecated for clarity, please use ContinueJobWith method with the same arguments. Will be removed in 2.0.0.")]
        public static string ContinueWith(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [InstantHandle] Expression<Action> methodCall,
            [NotNull] IState nextState,
            JobContinuationOptions options)
        {
            return ContinueJobWith(client, parentId, methodCall, nextState, options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered.</param>
        /// <param name="options">Continuation options.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueJobWith(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [InstantHandle] Expression<Action> methodCall,
            [NotNull] IState nextState,
            JobContinuationOptions options)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new AwaitingState(parentId, nextState, options);
            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered. 
        /// If null, then <see cref="EnqueuedState"/> is used.</param>
        /// <param name="options">Continuation options. By default, 
        /// <see cref="JobContinuationOptions.OnlyOnSucceededState"/> is used.</param>
        /// <returns>Unique identifier of a created job.</returns>
        [Obsolete("Deprecated for clarity, please use ContinueJobWith method with the same arguments. Will be removed in 2.0.0.")]
        public static string ContinueWith(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [InstantHandle] Expression<Func<Task>> methodCall,
            [CanBeNull] IState nextState = null,
            JobContinuationOptions options = JobContinuationOptions.OnlyOnSucceededState)
        {
            return ContinueJobWith(client, parentId, methodCall, nextState, options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered. 
        /// If null, then <see cref="EnqueuedState"/> is used.</param>
        /// <param name="options">Continuation options. By default, 
        /// <see cref="JobContinuationOptions.OnlyOnSucceededState"/> is used.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueJobWith(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [InstantHandle] Expression<Func<Task>> methodCall,
            [CanBeNull] IState nextState = null,
            JobContinuationOptions options = JobContinuationOptions.OnlyOnSucceededState)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new AwaitingState(parentId, nextState ?? new EnqueuedState(), options);
            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered.</param>
        /// <param name="options">Continuation options.</param>
        /// <returns>Unique identifier of a created job.</returns>
        [Obsolete("Deprecated for clarity, please use ContinueJobWith method with the same arguments. Will be removed in 2.0.0.")]
        public static string ContinueWith<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            [NotNull] IState nextState,
            JobContinuationOptions options)
        {
            return ContinueJobWith(client, parentId, methodCall, nextState, options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered.</param>
        /// <param name="options">Continuation options.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueJobWith<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall,
            [NotNull] IState nextState,
            JobContinuationOptions options)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new AwaitingState(parentId, nextState, options);
            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered. 
        /// If null, then <see cref="EnqueuedState"/> is used.</param>
        /// <param name="options">Continuation options. By default, 
        /// <see cref="JobContinuationOptions.OnlyOnSucceededState"/> is used.</param>
        /// <returns>Unique identifier of a created job.</returns>
        [Obsolete("Deprecated for clarity, please use ContinueJobWith method with the same arguments. Will be removed in 2.0.0.")]
        public static string ContinueWith<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            [CanBeNull] IState nextState = null,
            JobContinuationOptions options = JobContinuationOptions.OnlyOnSucceededState)
        {
            return ContinueJobWith(client, parentId, methodCall, nextState, options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be triggered.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="nextState">Next state for a job, when continuation is triggered. 
        /// If null, then <see cref="EnqueuedState"/> is used.</param>
        /// <param name="options">Continuation options. By default, 
        /// <see cref="JobContinuationOptions.OnlyOnSucceededState"/> is used.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueJobWith<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            [CanBeNull] IState nextState = null,
            JobContinuationOptions options = JobContinuationOptions.OnlyOnSucceededState)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new AwaitingState(parentId, nextState ?? new EnqueuedState(), options);
            return client.Create(Job.FromExpression(methodCall), state);
        }
    }
}
