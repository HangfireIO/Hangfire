// This file is part of Hangfire. Copyright © 2015 Hangfire OÜ.
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
using Hangfire.Common;
using Hangfire.Processing;

namespace Hangfire.Server
{
    internal sealed class CoreBackgroundJobPerformer : IBackgroundJobPerformer
    {
        internal static readonly Dictionary<Type, Func<PerformContext, object>> Substitutions
            = new Dictionary<Type, Func<PerformContext, object>>
            {
                { typeof (IJobCancellationToken), static x => x.CancellationToken },
                { typeof (CancellationToken), static x => x.CancellationToken.ShutdownToken },
                { typeof (PerformContext), static x => x }
            };

        private readonly JobActivator _activator;
        private readonly TaskScheduler _taskScheduler;

        public CoreBackgroundJobPerformer([NotNull] JobActivator activator, [CanBeNull] TaskScheduler taskScheduler)
        {
            _activator = activator ?? throw new ArgumentNullException(nameof(activator));
            _taskScheduler = taskScheduler;
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

        internal static void HandleJobPerformanceException(Exception exception, IJobCancellationToken cancellationToken, [CanBeNull] BackgroundJob job)
        {
            if (exception is JobAbortedException)
            {
                // JobAbortedException exception should be thrown as-is to notify
                // a worker that background job was aborted by a state change, and
                // should NOT be re-queued.
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
            
            if (exception is OperationCanceledException && cancellationToken.IsAborted())
            {
                // OperationCanceledException exception is thrown because 
                // ServerJobCancellationWatcher has detected the job was aborted.
                throw new JobAbortedException();
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

            // Other exceptions are wrapped with JobPerformanceException to preserve a
            // shallow stack trace without Hangfire methods.
            throw new JobPerformanceException(
                "An exception occurred during performance of the job.",
                exception, job?.Id);
        }

        private object InvokeMethod(PerformContext context, object instance, object[] arguments)
        {
            if (context.BackgroundJob.Job == null) return null;

            try
            {
                var methodInfo = context.BackgroundJob.Job.Method;
                var method = new BackgroundJobMethod(methodInfo, instance, arguments);
                var returnType = methodInfo.ReturnType;

                if (returnType.IsTaskLike(out var getTaskFunc))
                {
                    if (_taskScheduler != null)
                    {
                        return InvokeOnTaskScheduler(context, method, getTaskFunc);
                    }

                    return InvokeOnTaskPump(context, method, getTaskFunc);
                }

                return InvokeSynchronously(method);
            }
            catch (ArgumentException ex)
            {
                HandleJobPerformanceException(ex, context.CancellationToken, context.BackgroundJob);
                throw;
            }
            catch (AggregateException ex)
            {
                HandleJobPerformanceException(ex.InnerException, context.CancellationToken, context.BackgroundJob);
                throw;
            }
            catch (TargetInvocationException ex)
            {
                HandleJobPerformanceException(ex.InnerException, context.CancellationToken, context.BackgroundJob);
                throw;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                HandleJobPerformanceException(ex, context.CancellationToken, context.BackgroundJob);
                throw;
            }
        }

        private object InvokeOnTaskScheduler(PerformContext context, BackgroundJobMethod method, Func<object, Task> getTaskFunc)
        {
            var scheduledTask = Task.Factory.StartNew(
                InvokeSynchronously,
                method,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);

            var result = scheduledTask.GetAwaiter().GetResult();
            if (result == null) return null;

            return getTaskFunc(result).GetTaskLikeResult(result, method.ReturnType);
        }

        private static object InvokeOnTaskPump(PerformContext context, BackgroundJobMethod method, Func<object, Task> getTaskFunc)
        {
            // Using SynchronizationContext here is the best default option, where workers
            // are still running on synchronous dispatchers, and where a single job performer
            // may be used by multiple workers. We can't create a separate TaskScheduler
            // instance of every background job invocation, because TaskScheduler.Id may
            // overflow relatively fast, and can't use single scheduler for multiple performers
            // for better isolation in the default case – non-default external scheduler should
            // be used. It's also great to preserve backward compatibility for those who are
            // using Parallel.For(Each), since we aren't changing the TaskScheduler.Current.

            var oldSyncContext = SynchronizationContext.Current;

            try
            {
                using (var syncContext = new InlineSynchronizationContext())
                using (var cancellationEvent = context.CancellationToken.ShutdownToken.GetCancellationEvent())
                {
                    SynchronizationContext.SetSynchronizationContext(syncContext);

                    var result = InvokeSynchronously(method);
                    if (result == null) return null;

                    var task = getTaskFunc(result);
                    var asyncResult = (IAsyncResult)task;

                    var waitHandles = new[] { syncContext.WaitHandle, asyncResult.AsyncWaitHandle, cancellationEvent.WaitHandle };

                    while (!asyncResult.IsCompleted && WaitHandle.WaitAny(waitHandles) == 0)
                    {
                        var workItem = syncContext.Dequeue();
                        workItem.Item1(workItem.Item2);
                    }

                    return task.GetTaskLikeResult(result, method.ReturnType);
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldSyncContext);
            }
        }

        private static object InvokeSynchronously(object state)
        {
            var method = (BackgroundJobMethod) state;
            return method.Invoke();
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

                var value = Substitutions.TryGetValue(parameter.ParameterType, out var substitution) 
                    ? substitution(context) 
                    : argument;

                result.Add(value);
            }

            return result.ToArray();
        }

        private sealed class BackgroundJobMethod
        {
            private readonly MethodInfo _methodInfo;
            private readonly object _instance;
            private readonly object[] _parameters;

            public BackgroundJobMethod(MethodInfo methodInfo, object instance, object[] parameters)
            {
                _methodInfo = methodInfo;
                _instance = instance;
                _parameters = parameters;
            }

            public Type ReturnType => _methodInfo.ReturnType;

            public object Invoke()
            {
                return _methodInfo.Invoke(_instance, _parameters);
            }
        }
    }
}