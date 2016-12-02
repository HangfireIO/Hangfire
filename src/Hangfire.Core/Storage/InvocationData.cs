// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using Hangfire.Common;
using Hangfire.Server;

namespace Hangfire.Storage
{
    public class InvocationData
    {
        public InvocationData(
            string type, string method, string parameterTypes, string arguments)
        {
            Type = type;
            Method = method;
            ParameterTypes = parameterTypes;
            Arguments = arguments;
        }

        public string Type { get; }
        public string Method { get; }
        public string ParameterTypes { get; }
        public string Arguments { get; set; }

        public Job Deserialize()
        {
            try
            {
                var type = System.Type.GetType(Type, throwOnError: true, ignoreCase: true);
                var parameterTypes = SerializationHelper.Deserialize<Type[]>(ParameterTypes);
                var method = type.GetNonOpenMatchingMethod(Method, parameterTypes);
                
                if (method == null)
                {
                    throw new InvalidOperationException(
                        $"The type `{type.FullName}` does not contain a method with signature `{Method}({String.Join(", ", parameterTypes.Select(x => x.Name))})`");
                }

                var serializedArguments = SerializationHelper.Deserialize<string[]>(Arguments);
                var arguments = DeserializeArguments(method, serializedArguments);

                return new Job(type, method, arguments);
            }
            catch (Exception ex)
            {
                throw new JobLoadException("Could not load the job. See inner exception for the details.", ex);
            }
        }

        public static InvocationData Serialize(Job job)
        {
            return new InvocationData(
                job.Type.AssemblyQualifiedName,
                job.Method.Name,
                SerializationHelper.Serialize(job.Method.GetParameters().Select(x => x.ParameterType).ToArray()),
                SerializationHelper.Serialize(SerializeArguments(job.Args)));
        }

        internal static string[] SerializeArguments(IReadOnlyCollection<object> arguments)
        {
            var serializedArguments = new List<string>(arguments.Count);
            foreach (var argument in arguments)
            {
                string value;

                if (argument != null)
                {
                    if (argument is DateTime)
                    {
                        value = ((DateTime) argument).ToString("o", CultureInfo.InvariantCulture);
                    }
                    else if (argument is CancellationToken)
                    {
                        // CancellationToken type instances are substituted with ShutdownToken 
                        // during the background job performance, so we don't need to store 
                        // their values.
                        value = null;
                    }
                    else
                    {
                        value = SerializationHelper.Serialize(argument, SerializationOption.User);
                    }
                }
                else
                {
                    value = null;
                }

                // Logic, related to optional parameters and their default values, 
                // can be skipped, because it is impossible to omit them in 
                // lambda-expressions (leads to a compile-time error).

                serializedArguments.Add(value);
            }

            return serializedArguments.ToArray();
        }

        internal static object[] DeserializeArguments(MethodInfo methodInfo, string[] arguments)
        {
            var parameters = methodInfo.GetParameters();
            var result = new List<object>(arguments.Length);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var argument = arguments[i];

                object value;

                if (CoreBackgroundJobPerformer.Substitutions.ContainsKey(parameter.ParameterType))
                {
                    value = parameter.ParameterType.GetTypeInfo().IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null;
                }
                else
                {
                    value = DeserializeArgument(argument, parameter.ParameterType);
                }

                result.Add(value);
            }

            return result.ToArray();
        }

        private static object DeserializeArgument(string argument, Type type)
        {
            object value;
            try
            {
                value = argument != null
                    ? SerializationHelper.Deserialize(argument, type, SerializationOption.User)
                    : null;
            }
            catch (Exception
#if NETFULL
            jsonException
#endif
            )
            {
                if (type == typeof (object))
                {
                    // Special case for handling object types, because string can not
                    // be converted to object type.
                    value = argument;
                }
                else
                {
#if NETFULL
                    try
                    {
                        var converter = TypeDescriptor.GetConverter(type);

                        // ReferenceConverter can't correctly convert the serialized
                        // data. This may happen when FromJson method threw an exception,
                        // we should rethrow it instead of trying to deserialize.
                        if (converter.GetType() == typeof(ReferenceConverter))
                        {
                            ExceptionDispatchInfo.Capture(jsonException).Throw();
                            throw;
                        }

                        value = converter.ConvertFromInvariantString(argument);
                    }
                    catch (Exception)
                    {
                        ExceptionDispatchInfo.Capture(jsonException).Throw();
                        throw;
                    }
#else
                    DateTime dateTime;
                    if (type == typeof(DateTime) && ParseDateTimeArgument(argument, out dateTime))
                    {
                        value = dateTime;
                    }
                    else
                    {
                        throw;
                    }
#endif
                }
            }
            return value;
        }

        internal static bool ParseDateTimeArgument(string argument, out DateTime value)
        {
            DateTime dateTime;
            var result = DateTime.TryParse(argument, out dateTime);

            if (!result)
            {
                result = DateTime.TryParseExact(
                    argument, 
                    "MM/dd/yyyy HH:mm:ss.ffff", 
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.None, 
                    out dateTime);
            }

            value = dateTime;
            return result;
        }
    }
}
