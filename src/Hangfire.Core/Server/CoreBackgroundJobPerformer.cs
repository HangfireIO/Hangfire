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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;

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
        private readonly IJobFilterProvider _filterProvider;

        public CoreBackgroundJobPerformer(
            [NotNull] JobActivator activator,
            [NotNull] IJobFilterProvider filterProvider)
        {
            if (activator == null) throw new ArgumentNullException("activator");
            if (filterProvider == null) throw new ArgumentNullException("filterProvider");

            _activator = activator;
            _filterProvider = filterProvider;
        }

        public object Perform(PerformContext context)
        {
            using (var scope = _activator.BeginScope())
            {
                object instance = null;

                if (!context.BackgroundJob.Job.Method.IsStatic)
                {
                    var activationContext = new ActivationContext(context.Connection, context.BackgroundJob);
                    instance = CreateInstance(scope, activationContext);
                }

                var arguments = SubstituteArguments(context);
                var result = InvokeMethod(context.BackgroundJob.Job.Method, instance, arguments);

                return result;
            }
        }

        private object CreateInstance(JobActivatorScope scope, ActivationContext context)
        {
            var filterInfo = new JobFilterInfo(_filterProvider.GetFilters(context.BackgroundJob.Job));

            BeforeActivation(context, filterInfo.ActivationFilters);
            var activatedContext = ActivateJob(scope, context);
            AfterActivation(activatedContext, filterInfo.ActivationFilters);

            if (activatedContext.Exception != null)
            {
                throw activatedContext.Exception;
            }

            if (activatedContext.Instance == null)
            {
                throw new InvalidOperationException(
                    String.Format("JobActivator returned NULL instance of the '{0}' type.", context.BackgroundJob.Job.Type));
            }

            return activatedContext.Instance;
        }

        private void BeforeActivation(ActivationContext context, IEnumerable<IActivationFilter> activationFilters)
        {
            Action<ActivatingContext> onActivating = activationFilters.Aggregate(new Action<ActivatingContext>(c => { }),
                (previous, filter) => previous + (filter.OnActivating));

            try
            {
                onActivating(new ActivatingContext(context));
            }
            catch (Exception e)
            {
                throw new JobPerformanceException("An exception occurred during execution of one of the filters", e);
            }
        }

        private void AfterActivation(ActivatedContext context, IEnumerable<IActivationFilter> activationFilters)
        {
            Action<ActivatedContext> onActivated = activationFilters.Aggregate(new Action<ActivatedContext>(c => { }),
                (previous, filter) => previous + (filter.OnActivated));

            try
            {
                onActivated(context);
            }
            catch (Exception e)
            {
                throw new JobPerformanceException("An exception occurred during execution of one of the filters", e);
            }
        }

        private ActivatedContext ActivateJob(JobActivatorScope scope, ActivationContext context)
        {
            object instance;

            try
            {
                instance = scope.Resolve(context.BackgroundJob.Job.Type);
            }
            catch (Exception e)
            {
                return new ActivatedContext(context, null, e);
            }

            return new ActivatedContext(context, instance, null);
        }

        private static object InvokeMethod(MethodInfo methodInfo, object instance, object[] arguments)
        {
            try
            {
                return methodInfo.Invoke(instance, arguments);
            }
            catch (ArgumentException ex)
            {
                throw new JobPerformanceException(
                    "An exception occurred during performance of the job.",
                    ex);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is OperationCanceledException && !(ex.InnerException is TaskCanceledException))
                {
                    // `OperationCanceledException` and its descendants are used
                    // to notify a worker that job performance was canceled,
                    // so we should not wrap this exception and throw it as-is.
                    throw ex.InnerException;
                }

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