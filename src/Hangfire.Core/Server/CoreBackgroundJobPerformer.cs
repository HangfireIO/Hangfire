// This file is part of Hangfire.
// Copyright © 2015 Sergey Odinokov.
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
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;

namespace Hangfire.Server
{
    internal class CoreBackgroundJobPerformer : IBackgroundJobPerformer
    {
        internal static readonly Dictionary<Type, Func<PerformContext, object>> Substitutions
            = new Dictionary<Type, Func<PerformContext, object>>
            {
                { typeof (IJobCancellationToken), x => x.CancellationToken },
                { typeof (CancellationToken), x => x.CancellationToken.ShutdownToken },
                { typeof (PerformContext), x => x }
            };

        public object Perform(PerformContext context)
        {
            object instance = null;

            if (context.BackgroundJob.Job == null)
            {
                throw new InvalidOperationException("Can't perform a background job with a null job.");
            }
                
            if (!context.BackgroundJob.Job.Method.IsStatic)
            {
                instance = context.Resolve(context.BackgroundJob.Job.Type);

                if (instance == null)
                {
                    throw new InvalidOperationException(
                        $"JobActivator returned NULL instance of the '{context.BackgroundJob.Job.Type}' type.");
                }
            }

            var arguments = SubstituteArguments(context);
            var result = InvokeMethod(context, instance, arguments);

            return result;
        }

        internal static void HandleJobPerformanceException(Exception exception, CancellationToken shutdownToken)
        {
            if (exception is JobAbortedException)
            {
                // JobAbortedException exception should be thrown as-is to notify
                // a worker that background job was aborted by a state change, and
                // should NOT be re-queued.
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            if (exception is OperationCanceledException && shutdownToken.IsCancellationRequested)
            {
                // OperationCanceledException exceptions are treated differently from
                // others, when ShutdownToken's cancellation was requested, to notify
                // a worker that job performance was aborted by a shutdown request,
                // and a job identifier should BE re-queued.
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw exception;
            }

            // Other exceptions are wrapped with JobPerformanceException to preserve a
            // shallow stack trace without Hangfire methods.
            throw new JobPerformanceException(
                "An exception occurred during performance of the job.",
                exception);
        }

        private static object InvokeMethod(PerformContext context, object instance, object[] arguments)
        {
            try
            {
                var methodInfo = context.BackgroundJob.Job.Method;
                var result = methodInfo.Invoke(instance, arguments);

                var task = result as Task;

                if (task != null)
                {
                    task.Wait();

                    if (methodInfo.ReturnType.GetTypeInfo().IsGenericType)
                    {
                        var resultProperty = methodInfo.ReturnType.GetRuntimeProperty("Result");

                        result = resultProperty.GetValue(task);
                    }
                    else
                    {
                        result = null;
                    }
                }

                return result;
            }
            catch (ArgumentException ex)
            {
                HandleJobPerformanceException(ex, context.CancellationToken.ShutdownToken);
                throw;
            }
            catch (AggregateException ex)
            {
                HandleJobPerformanceException(ex.InnerException, context.CancellationToken.ShutdownToken);
                throw;
            }
            catch (TargetInvocationException ex)
            {
                HandleJobPerformanceException(ex.InnerException, context.CancellationToken.ShutdownToken);
                throw;
            }
        }

        private static object[] SubstituteArguments(PerformContext context)
        {
            if (context.BackgroundJob.Job == null)
            {
                return null;
            }

            var parameters = context.BackgroundJob.Job.Method.GetParameters();
            var result = new List<object>(context.BackgroundJob.Job.Args.Count);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var argument = context.BackgroundJob.Job.Args[i];

                var value = Substitutions.ContainsKey(parameter.ParameterType) 
                    ? Substitutions[parameter.ParameterType](context) 
                    : argument;

                result.Add(value);
            }

            return result.ToArray();
        }
    }
}