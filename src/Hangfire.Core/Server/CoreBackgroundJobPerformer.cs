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
using System.Runtime.CompilerServices;

namespace Hangfire.Server
{
    internal class CoreBackgroundJobPerformer : IBackgroundJobPerformer
    {
        internal static readonly Dictionary<Type, Func<PerformContext, object>> Substitutions
            = new Dictionary<Type, Func<PerformContext, object>>
            {
                { typeof (IJobCancellationToken), x => x.CancellationToken },
                { typeof (CancellationToken), x => x.CancellationToken.CancellationToken },
                { typeof (PerformContext), x => x }
            };

        private readonly JobActivator _activator;

        public CoreBackgroundJobPerformer([NotNull] JobActivator activator)
        {
            if (activator == null) throw new ArgumentNullException(nameof(activator));
            _activator = activator;
        }

        public object Perform(PerformContext context)
        {
            using (var scope = _activator.BeginScope(context))
            {
                object instance = null;

                if (context.BackgroundJob.Job == null)
                {
                    throw new InvalidOperationException("Can't perform a background job with a null job.");
                }
                
                if (!context.BackgroundJob.Job.Method.IsStatic)
                {
                    instance = scope.Resolve(context.BackgroundJob.Job.Type);

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
        }

        internal static void HandleJobPerformanceException(Exception exception, IJobCancellationToken cancellationToken)
        {
            if (exception is JobAbortedException)
            {
                // JobAbortedException exception should be thrown as-is to notify
                // a worker that background job was aborted by a state change, and
                // should NOT be re-queued.
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
            
            if (exception is OperationCanceledException && cancellationToken.ShutdownToken.IsCancellationRequested)
            {
                // OperationCanceledException exceptions are treated differently from
                // others, when ShutdownToken's cancellation was requested, to notify
                // a worker that job performance was aborted by a shutdown request,
                // and a job identifier should BE re-queued.
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw exception;
            }

            if (exception is OperationCanceledException && cancellationToken.IsAborted())
            {
                // OperationCanceledException exception is thrown because 
                // ServerJobCancellationWatcher has detected the job was aborted.
                throw new JobAbortedException();
            }

            // Other exceptions are wrapped with JobPerformanceException to preserve a
            // shallow stack trace without Hangfire methods.
            throw new JobPerformanceException(
                "An exception occurred during performance of the job.",
                exception);
        }

        private static readonly Type[] EmptyTypes = new Type[0];

        private static bool CheckAwaitable(Type type, out MethodInfo getAwaiter, out MethodInfo getResult)
        {
            // Starting with C# 7, async methods can return any type that has an accessible GetAwaiter method. 
            // The object returned by the GetAwaiter method must implement the ICriticalNotifyCompletion interface.
            // Ref: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/async-return-types
            // 
            // Other sources state that ICriticalNotifyCompletion is optional though:
            // Ref: https://blogs.msdn.microsoft.com/pfxteam/2012/04/12/asyncawait-faq/
            // Ref: https://codeblog.jonskeet.uk/category/eduasync/
            // Ref: http://putridparrot.com/blog/creating-awaitable-types/
            // 
            // Either way, INotifyCompletion is still required nevertheless.

            getAwaiter = null;
            getResult = null;

            // primitive types can't be awaitable
            if (type.GetTypeInfo().IsPrimitive) return false;
            
            // awaitable type must have a public parameterless GetAwaiter instance method, ...
            getAwaiter = type.GetRuntimeMethod("GetAwaiter", EmptyTypes);
            if (getAwaiter == null || getAwaiter.IsStatic || !getAwaiter.IsPublic) return false;
            
            var awaiterType = getAwaiter.ReturnType;
            
            // ... its return type must implement INotifyCompletion, ...
            if (!typeof(INotifyCompletion).GetTypeInfo().IsAssignableFrom(awaiterType.GetTypeInfo())) return false;

            // ... have a boolean IsCompleted instance property with a public getter, ...
            var isCompleted = awaiterType.GetRuntimeProperty("IsCompleted");
            if (isCompleted == null || isCompleted.PropertyType != typeof(bool)) return false;
            if (!isCompleted.CanRead || isCompleted.GetMethod.IsStatic || !isCompleted.GetMethod.IsPublic) return false;

            // ... and also have a public parameterless GetResult instance method
            getResult = awaiterType.GetRuntimeMethod("GetResult", EmptyTypes);
            return getResult != null && !getResult.IsStatic && getResult.IsPublic;
        }

        private static object InvokeMethod(PerformContext context, object instance, object[] arguments)
        {
            try
            {
                var methodInfo = context.BackgroundJob.Job.Method;
                var result = methodInfo.Invoke(instance, arguments);
                
                MethodInfo getAwaiter, getResult;
                if (result != null && CheckAwaitable(methodInfo.ReturnType, out getAwaiter, out getResult))
                {
                    var awaiter = getAwaiter.Invoke(result, null);
                    if (awaiter != null)
                    {
                        result = getResult.Invoke(awaiter, null);
                    }
                }

                return result;
            }
            catch (ArgumentException ex)
            {
                HandleJobPerformanceException(ex, context.CancellationToken);
                throw;
            }
            catch (AggregateException ex)
            {
                HandleJobPerformanceException(ex.InnerException, context.CancellationToken);
                throw;
            }
            catch (TargetInvocationException ex)
            {
                HandleJobPerformanceException(ex.InnerException, context.CancellationToken);
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