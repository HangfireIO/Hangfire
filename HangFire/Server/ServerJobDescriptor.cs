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

using ServiceStack.Redis;

namespace HangFire.Server
{
    public class ServerJobDescriptor : JobDescriptor, IDisposable
    {
        private readonly IRedisClient _redis;
        private object _jobInstance;

        private readonly Action _performJobCallback;

        internal ServerJobDescriptor(
            IRedisClient redis,
            JobActivator activator,
            JobPayload payload)
            : base(payload.Id, payload.Type)
        {
            _redis = redis;
            if (activator == null) throw new ArgumentNullException("activator");
            if (payload == null) throw new ArgumentNullException("payload");

            if (Type == null)
            {
                throw new InvalidOperationException("Could not instantiate the job. See the inner exception for details.", TypeLoadException);
            }

            if (String.IsNullOrWhiteSpace(payload.Method))
            {
                // Old background job invocation type, based on a BackgroundJob class.
                // We need to initialize instance properties here.
                ActivateJob(activator);
                InitializePropertiesFromArguments(payload);

                _performJobCallback = () => ((BackgroundJob)_jobInstance).Perform();
            }
            else
            {
                // New job invocation type, based on a given method. We need to find it first.
                // TODO: wrap parameters in a custom class
                var serialiedArgs = JobHelper.FromJson<List<ArgumentPair>>(payload.Args);
                var argumentTypes = new Type[serialiedArgs.Count];
                var arguments = new object[serialiedArgs.Count];

                for (int i = 0; i < serialiedArgs.Count; i++)
                {
                    var serializedArg = serialiedArgs[i];
                    object value;

                    if (serializedArg.Type == typeof(object))
                    {
                        // Special case for handling object types, because string can not
                        // be converted to object type.
                        value = serializedArg.Value;
                    }
                    else
                    {
                        var converter = TypeDescriptor.GetConverter(serializedArg.Type);
                        value = converter.ConvertFromInvariantString(serializedArg.Value);
                    }

                    argumentTypes[i] = serializedArg.Type;
                    arguments[i] = value;
                }

                var methodInfo = Type.GetMethod(payload.Method, argumentTypes);

                if (!methodInfo.IsStatic)
                {
                    ActivateJob(activator);
                }

                _performJobCallback = () => methodInfo.Invoke(_jobInstance, arguments);
            }
        }

        private void ActivateJob(JobActivator activator)
        {
            _jobInstance = activator.ActivateJob(Type);

            if (_jobInstance == null)
            {
                throw new InvalidOperationException(
                    String.Format("{0} returned NULL instance of the '{1}' type.", activator.GetType().FullName, Type.FullName));
            }
        }

        public void SetParameter(string name, object value)
        {
            _redis.SetEntryInHash(
                String.Format("hangfire:job:{0}", JobId),
                name,
                JobHelper.ToJson(value));
        }

        public T GetParameter<T>(string name)
        {
            var value = _redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", JobId),
                name);

            return JobHelper.FromJson<T>(value);
        }

        internal void Perform()
        {
            _performJobCallback();
        }

        internal void Dispose()
        {
            var disposable = _jobInstance as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }

        private void InitializePropertiesFromArguments(JobPayload payload)
        {
            var args = JobHelper.FromJson<Dictionary<string, string>>(payload.Args);

            foreach (var arg in args)
            {
                var propertyInfo = _jobInstance.GetType().GetProperty(arg.Key);
                if (propertyInfo != null)
                {
                    var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);

                    try
                    {
                        var value = converter.ConvertFromInvariantString(arg.Value);
                        propertyInfo.SetValue(_jobInstance, value, null);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                "Could not set the property '{0}' of the instance of class '{1}'. See the inner exception for details.",
                                propertyInfo.Name,
                                _jobInstance.GetType().Name),
                            ex);
                    }
                }
            }
        }
    }
}
