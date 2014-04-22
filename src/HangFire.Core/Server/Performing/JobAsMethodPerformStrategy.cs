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
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using HangFire.Common;

namespace HangFire.Server.Performing
{
    internal class JobAsMethodPerformStrategy : IJobPerformStrategy
    {
        private readonly Job _job;
        private readonly JobActivator _activator = JobActivator.Current;

        public JobAsMethodPerformStrategy(Job job)
            : this(job, JobActivator.Current)
        {
        }

        public JobAsMethodPerformStrategy(Job job, JobActivator activator)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (activator == null) throw new ArgumentNullException("activator");

            _job = job;
            _activator = activator;
        }

        public void Perform()
        {
            object instance = null;

            try
            {
                if (!_job.MethodData.MethodInfo.IsStatic)
                {
                    instance = ActivateJob();
                }

                var deserializedArguments = DeserializeArguments();
                InvokeMethod(instance, deserializedArguments);
            }
            finally
            {
                Dispose(instance);
            }
        }

        private object ActivateJob()
        {
            try
            {
                var instance = _activator.ActivateJob(_job.MethodData.Type);

                if (instance == null)
                {
                    throw new InvalidOperationException(
                        String.Format("JobActivator returned NULL instance of the '{0}' type.", _job.MethodData.Type));
                }

                return instance;
            }
            catch (Exception ex)
            {
                throw new JobPerformanceException(
                    "An exception occured during job activation.",
                    ex);
            }
        }

        private object[] DeserializeArguments()
        {
            try
            {
                var parameters = _job.MethodData.MethodInfo.GetParameters();
                var result = new List<object>(_job.Arguments.Length);

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var argument = _job.Arguments[i];

                    object value;

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

                    result.Add(value);
                }

                return result.ToArray();
            }
            catch (Exception ex)
            {
                throw new JobPerformanceException(
                    "An exception occured during arguments deserialization.",
                    ex);
            }
        }

        private void InvokeMethod(object instance, object[] deserializedArguments)
        {
            try
            {
                _job.MethodData.MethodInfo.Invoke(instance, deserializedArguments);
            }
            catch (TargetInvocationException ex)
            {
                throw new JobPerformanceException(
                    "An exception occurred during performance of the job.",
                    ex.InnerException);
            }
        }

        private static void Dispose(object instance)
        {
            try
            {
                var disposable = instance as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new JobPerformanceException(
                    "Job has been performed, but an exception occured during disposal.",
                    ex);
            }
        }
    }
}
