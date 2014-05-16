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
using HangFire.States;

namespace HangFire
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
        public static string Enqueue(this IBackgroundJobClient client, Expression<Action> methodCall)
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
        public static string Enqueue<T>(this IBackgroundJobClient client, Expression<Action<T>> methodCall)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(methodCall, new EnqueuedState());
        }

        /// <summary>
        /// Creates a background job based on a specified static method 
        /// call expression and places it into specified queue. 
        /// Please, note that the <see cref="QueueAttribute"/> can
        /// override the specified queue.
        /// </summary>
        /// 
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="queue">Queue to which the job will be placed.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue(this IBackgroundJobClient client, Expression<Action> methodCall, string queue)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(methodCall, new EnqueuedState(queue));
        }

        /// <summary>
        /// Creates a background job based on a specified instance method 
        /// call expression and places it into specified queue. 
        /// Please, note that the <see cref="QueueAttribute"/> can
        /// override the specified queue.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="queue">Queue to which the job will be placed.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Enqueue<T>(
            this IBackgroundJobClient client, Expression<Action<T>> methodCall, string queue)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(methodCall, new EnqueuedState(queue));
        }

        /// <summary>
        /// Creates a new background job based on a specified static method
        /// call expression and schedules it to be enqueued after a given delay.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule(this IBackgroundJobClient client, Expression<Action> methodCall, TimeSpan delay)
        {
            if (client == null) throw new ArgumentNullException("client");

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
        public static string Schedule<T>(this IBackgroundJobClient client, Expression<Action<T>> methodCall, TimeSpan delay)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(methodCall, new ScheduledState(delay));
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
            this IBackgroundJobClient client,
            Expression<Action> methodCall,
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
            this IBackgroundJobClient client,
            Expression<Action<T>> methodCall,
            IState state)
        {
            if (client == null) throw new ArgumentNullException("client");

            return client.Create(Job.FromExpression(methodCall), state);
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
        /// <param name="jobId">An identifier, that will be used to find a job.</param>
        /// <returns>True on a successfull state transition, false otherwise.</returns>
        public static bool Delete(this IBackgroundJobClient client, string jobId)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (jobId == null) throw new ArgumentNullException("jobId");

            var state = new DeletedState();

            var stateMachine = client.StateMachineFactory.Create(client.Connection);
            return stateMachine.TryToChangeState(jobId, state, null);
        }
    }
}
