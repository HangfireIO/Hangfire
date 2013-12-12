// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
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
using HangFire.Client;
using ServiceStack.Redis;

namespace HangFire.Server
{
    public class ServerJobDescriptor : ServerJobDescriptorBase
    {
        private readonly JobActivator _activator;
        private readonly string[] _arguments;

        internal ServerJobDescriptor(
            IRedisClient redis,
            JobActivator activator,
            string jobId,
            JobInvocationData data,
            string[] arguments)
            : base(redis, jobId, data)
        {
            if (activator == null) throw new ArgumentNullException("activator");
            if (data == null) throw new ArgumentNullException("data");
            if (arguments == null) throw new ArgumentNullException("arguments");

            _activator = activator;
            _arguments = arguments;
        }

        internal override void Perform()
        {
            object instance = null;

            try
            {
                if (!InvocationData.Method.IsStatic)
                {
                    instance = ActivateJob();
                }

                var parameters = InvocationData.Method.GetParameters();
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

                InvocationData.Method.Invoke(instance, deserializedArguments.ToArray());
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
            var instance = _activator.ActivateJob(InvocationData.Type);

            if (instance == null)
            {
                throw new InvalidOperationException(
                    String.Format("JobActivator returned NULL instance of the '{0}' type.", InvocationData.Type));
            }

            return instance;
        }
    }
}
