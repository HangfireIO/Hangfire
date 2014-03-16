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
using System.Reflection;
using HangFire.Common;

namespace HangFire.Server.Performing
{
    internal class JobAsMethodPerformStrategy : IJobPerformStrategy
    {
        private readonly JobActivator _activator = JobActivator.Current;

        private readonly MethodData _methodData;
        private readonly string[] _arguments;

        public JobAsMethodPerformStrategy(
            MethodData methodData,
            string[] arguments)
        {
            if (methodData == null) throw new ArgumentNullException("methodData");
            if (arguments == null) throw new ArgumentNullException("arguments");

            _methodData = methodData;
            _arguments = arguments;
        }

        public void Perform()
        {
            object instance = null;

            try
            {
                if (!_methodData.MethodInfo.IsStatic)
                {
                    instance = ActivateJob();
                }

                var parameters = _methodData.MethodInfo.GetParameters();
                var deserializedArguments = new List<object>(_arguments.Length);

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var argument = _arguments[i];

                    object value;

                    if (parameter.ParameterType == typeof (object))
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

                    deserializedArguments.Add(value);
                }

                try
                {
                    _methodData.MethodInfo.Invoke(instance, deserializedArguments.ToArray());
                }
                catch (TargetInvocationException ex)
                {
                    throw new JobPerformanceException(
                        "An exception occured during performance of the job",
                        ex.InnerException);
                }
            }
            finally
            {
                var disposable = instance as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }

        private object ActivateJob()
        {
            var instance = _activator.ActivateJob(_methodData.Type);

            if (instance == null)
            {
                throw new InvalidOperationException(
                    String.Format("JobActivator returned NULL instance of the '{0}' type.", _methodData.Type));
            }

            return instance;
        }
    }
}
