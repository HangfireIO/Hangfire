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
#if !NETSTANDARD1_3
using System.ComponentModel;
#endif
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Newtonsoft.Json;

namespace Hangfire.Storage
{
    public class InvocationData
    {
        private static readonly object[] EmptyArray = new object[0];

        [Obsolete("Please use IGlobalConfiguration.UseTypeResolver instead. Will be removed in 2.0.0.")]
        public static void SetTypeResolver([CanBeNull] Func<string, Type> typeResolver)
        {
            TypeHelper.CurrentTypeResolver = typeResolver;
        }

        [Obsolete("Please use IGlobalConfiguration.UseTypeSerializer instead. Will be removed in 2.0.0.")]
        public static void SetTypeSerializer([CanBeNull] Func<Type, string> typeSerializer)
        {
            TypeHelper.CurrentTypeSerializer = typeSerializer;
        }

        public InvocationData(string type, string method, string parameterTypes, string arguments)
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

        [Obsolete("Please use DeserializeJob() method instead. Will be removed in 2.0.0 for clarity.")]
        public Job Deserialize()
        {
            return DeserializeJob();
        }

        [Obsolete("Please use SerializeJob(Job) method instead. Will be removed in 2.0.0 for clarity.")]
        public static InvocationData Serialize(Job job)
        {
            return SerializeJob(job);
        }

        public Job DeserializeJob()
        {
            var typeResolver = TypeHelper.CurrentTypeResolver;

            try
            {
                var type = typeResolver(Type);

                var parameterTypesArray = DeserializeParameterTypesArray();
                var parameterTypes = parameterTypesArray?.Select(typeResolver).ToArray();

                var method = type.GetNonOpenMatchingMethod(Method, parameterTypes);

                if (method == null)
                {
                    var parametersString = parameterTypes != null
                        ? String.Join(", ", parameterTypes.Select(x => x.Name))
                        : ParameterTypes ?? String.Empty;
                    
                    throw new InvalidOperationException(
                        $"The type `{type.FullName}` does not contain a method with signature `{Method}({parametersString})`");
                }

                var argumentsArray = SerializationHelper.Deserialize<string[]>(Arguments);
                var arguments = DeserializeArguments(method, argumentsArray);

                return new Job(type, method, arguments);
            }
            catch (Exception ex)
            {
                throw new JobLoadException("Could not load the job. See inner exception for the details.", ex);
            }
        }

        public static InvocationData SerializeJob(Job job)
        {
            var typeSerializer = TypeHelper.CurrentTypeSerializer;

            var type = typeSerializer(job.Type);
            var methodName = job.Method.Name;
            var parameterTypes = SerializationHelper.Serialize(
                job.Method.GetParameters().Select(x => typeSerializer(x.ParameterType)).ToArray());
            var arguments = SerializationHelper.Serialize(SerializeArguments(job.Method, job.Args));

            return new InvocationData(type, methodName, parameterTypes, arguments);
        }

        public static InvocationData DeserializePayload(string payload)
        {
            JobPayload jobPayload = null;
            Exception exception = null;

            try
            {
                jobPayload = SerializationHelper.Deserialize<JobPayload>(payload);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception == null && jobPayload.TypeName != null && jobPayload.MethodName != null)
            {
                return new InvocationData(
                    jobPayload.TypeName,
                    jobPayload.MethodName,
                    SerializationHelper.Serialize(jobPayload.ParameterTypes),
                    SerializationHelper.Serialize(jobPayload.Arguments));
            }

            var data = SerializationHelper.Deserialize<InvocationData>(payload);

            if (data.Type == null || data.Method == null)
            {
                data = SerializationHelper.Deserialize<InvocationData>(payload, SerializationOption.User);
            }

            return data;
        }

        public string SerializePayload(bool excludeArguments = false)
        {
            if (GlobalConfiguration.HasCompatibilityLevel(CompatibilityLevel.Version_170))
            {
                var parameterTypes = DeserializeParameterTypesArray();
                var arguments = excludeArguments ? null : SerializationHelper.Deserialize<string[]>(Arguments);

                return SerializationHelper.Serialize(new JobPayload
                {
                    TypeName = Type,
                    MethodName = Method,
                    ParameterTypes = parameterTypes?.Length > 0 ? parameterTypes : null,
                    Arguments = arguments?.Length > 0 ? arguments : null
                });
            }

            return SerializationHelper.Serialize(excludeArguments
                ? new InvocationData(Type, Method, ParameterTypes, null)
                : this);
        }

        private string[] DeserializeParameterTypesArray()
        {
            try
            {
                return SerializationHelper.Deserialize<string[]>(ParameterTypes);
            }
            catch (Exception outerException)
            {
                try
                {
                    var parameterTypes = SerializationHelper.Deserialize<Type[]>(ParameterTypes);
                    return parameterTypes.Select(TypeHelper.CurrentTypeSerializer).ToArray();
                }
                catch (Exception)
                {
                    ExceptionDispatchInfo.Capture(outerException).Throw();
                    throw;
                }
            }
        }

        internal static string[] SerializeArguments(MethodInfo methodInfo, IReadOnlyList<object> arguments)
        {
            var serializedArguments = new List<string>(arguments.Count);
            var parameters = methodInfo.GetParameters();

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var argument = arguments[i];

                string value;

                if (argument != null)
                {
                    if (!GlobalConfiguration.HasCompatibilityLevel(CompatibilityLevel.Version_170) &&
                        argument is DateTime dateTime)
                    {
                        value = dateTime.ToString("o", CultureInfo.InvariantCulture);
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
                        value = SerializationHelper.Serialize(
                            argument,
                            parameter.ParameterType,
                            SerializationOption.User);
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
            if (arguments == null) return EmptyArray;

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
                value = SerializationHelper.Deserialize(argument, type, SerializationOption.User);
            }
            catch (Exception
#if !NETSTANDARD1_3
            jsonException
#endif
            )
            {
                if (type == typeof(object))
                {
                    // Special case for handling object types, because string can not
                    // be converted to object type.
                    value = argument;
                }
                else if ((type == typeof(DateTime) || type == typeof(DateTime?)) && ParseDateTimeArgument(argument, out var dateTime))
                {
                    value = dateTime;
                }
                else
                {
#if !NETSTANDARD1_3
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
                    throw;
#endif
                }
            }
            return value;
        }

        internal static bool ParseDateTimeArgument(string argument, out DateTime value)
        {
            var result = DateTime.TryParseExact(
                argument,
                "MM/dd/yyyy HH:mm:ss.ffff",
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out var dateTime);

            if (!result)
            {
                result = DateTime.TryParse(
                    argument,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out dateTime);
            }

            value = dateTime;
            return result;
        }

        private class JobPayload
        {
            [JsonProperty("t")]
            public string TypeName { get; set; }

            [JsonProperty("m")]
            public string MethodName { get; set; }

            [JsonProperty("p", NullValueHandling = NullValueHandling.Ignore)]
            public string[] ParameterTypes { get; set; }

            [JsonProperty("a", NullValueHandling = NullValueHandling.Ignore)]
            public string[] Arguments { get; set; }
        }
    }
}