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
using System.ComponentModel;
using System.Reflection;
using Hangfire.Annotations;
using Hangfire.Common;

namespace Hangfire.Server
{
    public class CoreJobPerformanceProcess : IJobPerformanceProcess
    {
        private readonly JobActivator _activator;

        public CoreJobPerformanceProcess()
            : this(JobActivator.Current)
        {
        }

        public CoreJobPerformanceProcess([NotNull] JobActivator activator)
        {
            if (activator == null) throw new ArgumentNullException("activator");
            _activator = activator;
        }

        public object Run(PerformContext context)
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

                var deserializedArguments = DeserializeArguments(context);
                var result = InvokeMethod(context.BackgroundJob.Job.Method, instance, deserializedArguments);

                return result;
            }
        }

        private object InvokeMethod(MethodInfo methodInfo, object instance, object[] deserializedArguments)
        {
            try
            {
                return methodInfo.Invoke(instance, deserializedArguments);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is OperationCanceledException)
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

        private object[] DeserializeArguments(PerformContext context)
        {
            try
            {
                var parameters = context.BackgroundJob.Job.Method.GetParameters();
                var result = new List<object>(context.BackgroundJob.Job.Arguments.Length);

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var argument = context.BackgroundJob.Job.Arguments[i];

                    object value;

                    if (typeof(IJobCancellationToken).IsAssignableFrom(parameter.ParameterType))
                    {
                        value = context.CancellationToken;
                    }
                    else
                    {
                        try
                        {
                            value = argument != null
                                ? JobHelper.FromJson(argument, parameter.ParameterType)
                                : null;
                        }
                        catch (Exception)
                        {
                            if (parameter.ParameterType == typeof(object))
                            {
                                // Special case for handling object types, because string can not
                                // be converted to object type.
                                value = argument;
                            }
                            else
                            {
                                var converter = TypeDescriptor.GetConverter(parameter.ParameterType);
                                value = converter.ConvertFromInvariantString(argument);
                            }
                        }
                    }

                    result.Add(value);
                }

                return result.ToArray();
            }
            catch (Exception ex)
            {
                throw new JobPerformanceException(
                    "An exception occurred during arguments deserialization.",
                    ex);
            }
        }
    }
}