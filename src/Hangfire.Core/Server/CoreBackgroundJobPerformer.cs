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
                { typeof (CancellationToken), x => x.CancellationToken.ShutdownToken }
            };

        private readonly JobActivator _activator;

        public CoreBackgroundJobPerformer([NotNull] JobActivator activator)
        {
            if (activator == null) throw new ArgumentNullException("activator");
            _activator = activator;
        }

        public object Perform(PerformContext context)
        {
            using (var scope = _activator.BeginScope())
            {
                object instance = null;

                if (!context.BackgroundJob.Job.Method.IsStatic)
                {
                    instance = scope.Resolve(context.BackgroundJob.Job.Type);

                    if (instance == null)
                    {
                        throw new InvalidOperationException(
                            String.Format("JobActivator returned NULL instance of the '{0}' type.", context.BackgroundJob.Job.Type));
                    }
                }

                var arguments = SubstituteArguments(context);
                var result = InvokeMethod(context, instance, arguments);

                return result;
            }
        }

        private static object InvokeMethod(PerformContext context, object instance, object[] arguments)
        {
            try
            {
                return context.BackgroundJob.Job.Method.Invoke(instance, arguments);
            }
            catch (ArgumentException ex)
            {
                throw new JobPerformanceException(
                    "An exception occurred during performance of the job.",
                    ex);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is JobAbortedException)
                {
                    // JobAbortedException exception should be thrown as-is to notify
                    // a worker that background job was aborted by a state change, and
                    // should NOT be re-queued.
                    throw ex.InnerException;
                }
                
                if (ex.InnerException is OperationCanceledException &&
                    context.CancellationToken.ShutdownToken.IsCancellationRequested)
                {
                    // OperationCanceledException exceptions are treated differently from
                    // others, when ShutdownToken's cancellation was requested, to notify
                    // a worker that job performance was aborted by a shutdown request,
                    // and a job identifier should BE re-queued.
                    throw ex.InnerException;
                }

                // Other exceptions are wrapped with JobPerformanceException to preserve a
                // shallow stack trace without Hangfire methods.
                throw new JobPerformanceException(
                    "An exception occurred during performance of the job.",
                    ex.InnerException);
            }
        }

        private static object[] SubstituteArguments(PerformContext context)
        {
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