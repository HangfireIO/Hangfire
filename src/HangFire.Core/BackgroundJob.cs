// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using HangFire.Client;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;

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
            return Create(methodCall, CreateEnqueuedState());
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
            return Create(methodCall, CreateEnqueuedState());
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
            return Create(methodCall, CreateScheduledState(delay));
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
            return Create(methodCall, CreateScheduledState(delay));
        }

        /// <summary>
        /// Creates a background job based on a specified static method call 
        /// expression in a given state and places it into the storage.
        /// </summary>
        /// 
        /// <param name="methodCall">Static method call expression that will be marshalled to the Server.</param>
        /// <param name="state">The state in which the job will be created.</param>
        /// <returns>Unique identifier of the created job.</returns>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="methodCall"/> does not contain <see cref="MethodCallExpression"/>.
        /// </exception>
        /// <exception cref="CreateJobFailedException">Job creation has failed.</exception>
        public static string Create(Expression<Action> methodCall, JobState state)
        {
            if (methodCall == null) throw new ArgumentNullException("methodCall");
            if (state == null) throw new ArgumentNullException("state");

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new ArgumentException("Должен указывать на метод", "methodCall");
            }

            // Static methods can not be overrided in the derived classes, 
            // so we can take the method's declaring type.
            return CreateInternal(callExpression.Method.DeclaringType, callExpression, state);
        }

        /// <summary>
        /// Creates a background job based on a specified instance method call 
        /// expression in a given state and places it into the storage.
        /// </summary>
        /// 
        /// <typeparam name="TJob">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="state">The state in which the job will be created.</param>
        /// <returns>Unique identifier of the job.</returns>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="methodCall"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="methodCall"/> does not contain <see cref="MethodCallExpression"/>.
        /// </exception>
        /// <exception cref="CreateJobFailedException">Job creation has failed.</exception>
        public static string Create<TJob>(Expression<Action<TJob>> methodCall, JobState state)
        {
            if (methodCall == null) throw new ArgumentNullException("methodCall");
            if (state == null) throw new ArgumentNullException("state");

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new ArgumentException("Должен указывать на метод", "methodCall");
            }

            // We should use exact type from the type parameter
            // that was specified by user, rather than declaring 
            // type of the given method.
            return CreateInternal(typeof(TJob), callExpression, state);
        }

        private static JobState CreateEnqueuedState()
        {
            return new EnqueuedState("Enqueued by the Сlient");
        }

        private static JobState CreateScheduledState(TimeSpan delay)
        {
            return CreateScheduledState(DateTime.UtcNow.Add(delay));
        }

        private static JobState CreateScheduledState(DateTime utcDateTime)
        {
            return new ScheduledState("Scheduled by the Client", utcDateTime);
        }

        private static string CreateInternal(Type type, MethodCallExpression callExpression, JobState state)
        {
            var arguments = GetArguments(callExpression);

            using (var client = ClientFactory())
            {
                var data = new JobMethod(type, callExpression.Method);
                return client.CreateJob(data, arguments, state);
            }
        }

        private static string[] GetArguments(MethodCallExpression callExpression)
        {
            var arguments = callExpression.Arguments.Select(GetArgumentValue).ToArray();

            var serializedArguments = new List<string>(arguments.Length);
            foreach (var argument in arguments)
            {
                string value = null;

                if (argument != null)
                {
                    var converter = TypeDescriptor.GetConverter(argument.GetType());
                    value = converter.ConvertToInvariantString(argument);
                }

                // Logic, related to optional parameters and their default values, 
                // can be skipped, because it is impossible to omit them in 
                // lambda-expressions (leads to a compile-time error).

                serializedArguments.Add(value);
            }

            return serializedArguments.ToArray();
        }

        private static object GetArgumentValue(Expression expression)
        {
            var constantExpression = expression as ConstantExpression;

            if (constantExpression != null)
            {
                return constantExpression.Value;
            }

            return CachedExpressionCompiler.Evaluate(expression);
        }
    }
}
