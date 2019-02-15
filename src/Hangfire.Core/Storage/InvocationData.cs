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
using System.Collections.Concurrent;
using System.Collections.Generic;
#if !NETSTANDARD1_3
using System.ComponentModel;
#endif
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
#if !NETSTANDARD1_3
using System.Runtime.ExceptionServices;
#endif
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;

namespace Hangfire.Storage
{
    public class InvocationData
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None
        };

        private static Func<string, Type> _typeResolver;
        private static Func<Type, string> _typeSerializer;

        public static void SetTypeResolver([CanBeNull] Func<string, Type> typeResolver)
        {
            Volatile.Write(ref _typeResolver, typeResolver);
        }

        public static void SetTypeSerializer([CanBeNull] Func<Type, string> typeSerializer)
        {
            Volatile.Write(ref _typeSerializer, typeSerializer);
        }

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
            var typeResolver = Volatile.Read(ref _typeResolver) ?? DefaultTypeResolver;

            try
            {
                var type = typeResolver(Type);
                var parameterTypes = GetParameterTypes(typeResolver);
                var method = type.GetNonOpenMatchingMethod(Method, parameterTypes);

                if (method == null)
                {
                    throw new InvalidOperationException(
                        $"The type `{type.FullName}` does not contain a method with signature `{Method}({String.Join(", ", parameterTypes.Select(x => x.Name))})`");
                }

                var serializedArguments = JobHelper.FromJson<string[]>(Arguments);
                var arguments = serializedArguments != null ? DeserializeArguments(method, serializedArguments) : new object[0];

                return new Job(type, method, arguments);
            }
            catch (Exception ex)
            {
                throw new JobLoadException("Could not load the job. See inner exception for the details.", ex);
            }
        }

        public static InvocationData Serialize(Job job)
        {
            var typeSerializer = Volatile.Read(ref _typeSerializer) ?? DefaultTypeSerializer;

            var type = typeSerializer(job.Type);
            var methodName = job.Method.Name;
            var parameterTypes = JsonConvert.SerializeObject(
                job.Method.GetParameters().Select(x => typeSerializer(x.ParameterType)).ToArray(),
                SerializerSettings);
            var arguments = JobHelper.ToJson(SerializeArguments(job.Args));

            return new InvocationData(type, methodName, parameterTypes, arguments);
        }

        public static InvocationData Deserialize(string serializedData)
        {
            var payload = JsonConvert.DeserializeObject<JobPayload>(serializedData, SerializerSettings);

            if (payload.TypeName != null && payload.MethodName != null)
            {
                return new InvocationData(
                    payload.TypeName,
                    payload.MethodName,
                    payload.ParameterTypes?.Length > 0 ? JsonConvert.SerializeObject(payload.ParameterTypes, SerializerSettings) : null,
                    JobHelper.ToJson(payload.Arguments));
            }

            return JobHelper.FromJson<InvocationData>(serializedData);
        }

        public string Serialize()
        {
            var parameterTypes = JsonConvert.DeserializeObject<string[]>(ParameterTypes, SerializerSettings);
            var arguments = JobHelper.FromJson<string[]>(Arguments);

            return JsonConvert.SerializeObject(new JobPayload
            {
                TypeName = Type,
                MethodName = Method,
                ParameterTypes = parameterTypes != null && parameterTypes.Length > 0 ? parameterTypes : null,
                Arguments = arguments != null && arguments.Length > 0 ? arguments : null
            }, SerializerSettings);
        }

        private Type[] GetParameterTypes(Func<string, Type> typeResolver)
        {
            if (ParameterTypes == null) return null;

            try
            {
                var parameterTypes = JsonConvert.DeserializeObject<string[]>(ParameterTypes, SerializerSettings);
                return parameterTypes?.Select(typeResolver).ToArray();
            }
            catch (JsonSerializationException)
            {
                return JsonConvert.DeserializeObject<Type[]>(ParameterTypes, SerializerSettings);
            }
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
                        value = ((DateTime)argument).ToString("o", CultureInfo.InvariantCulture);
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
                        value = JobHelper.ToJson(argument);
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
                    ? JobHelper.FromJson(argument, type)
                    : null;
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
                else
                {
                    DateTime dateTime;
                    if (ParseDateTimeArgument(argument, out dateTime))
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
                result = DateTime.TryParse(argument, null, DateTimeStyles.RoundtripKind, out dateTime);
            }

            value = dateTime;
            return result;
        }

        private static string DefaultTypeSerializer(Type type)
        {
            return type.AssemblyQualifiedName;
        }

        private static readonly ConcurrentDictionary<Type, string> TypeSerializerCache = new ConcurrentDictionary<Type, string>();

        internal static string SimpleAssemblyNameTypeSerializer(Type type)
        {
            return TypeSerializerCache.GetOrAdd(type, value =>
            {
                var builder = new StringBuilder();
                SerializeType(value, true, builder);

                return builder.ToString();
            });
        }

        private static void SerializeType(Type type, bool withAssemblyName, StringBuilder typeNameBuilder)
        {
            if (type == typeof(System.Console))
            {
                typeNameBuilder.Append("System.Console, mscorlib");
                return;
            }

            if (type == typeof(System.Threading.Thread))
            {
                typeNameBuilder.Append("System.Threading.Thread, mscorlib");
                return;
            }

            if (type.DeclaringType != null)
            {
                SerializeType(type.DeclaringType, false, typeNameBuilder);
                typeNameBuilder.Append('+');
            }
            else if (type.Namespace != null)
            {
                typeNameBuilder.Append(type.Namespace).Append('.');
            }

            typeNameBuilder.Append(type.Name);

            if (type.GenericTypeArguments.Length > 0)
            {
                SerializeTypes(type.GenericTypeArguments, typeNameBuilder);
            }

            if (!withAssemblyName) return;

            var typeInfo = type.GetTypeInfo();

            if (type != typeof(object) && type != typeof(string) && !typeInfo.IsPrimitive)
            {
                string assemblyName;

                var typeForwardedFrom = typeInfo.GetCustomAttribute<TypeForwardedFromAttribute>();
                if (typeForwardedFrom != null)
                {
                    assemblyName = typeForwardedFrom.AssemblyFullName;

                    var delimiterIndex = assemblyName.IndexOf(",", StringComparison.OrdinalIgnoreCase);

                    assemblyName = delimiterIndex >= 0 ? assemblyName.Substring(0, delimiterIndex) : assemblyName;
                }
                else
                {
                    assemblyName = typeInfo.Assembly.GetName().Name;
                }

                if (assemblyName.Equals("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyName = "mscorlib";
                }

                typeNameBuilder.Append(", ").Append(assemblyName);
            }
        }

        private static void SerializeTypes(Type[] types, StringBuilder typeNamesBuilder)
        {
            if (types == null) return;

            typeNamesBuilder.Append('[');

            for (var i = 0; i < types.Length; i++)
            {
                typeNamesBuilder.Append('[');
                SerializeType(types[i], true, typeNamesBuilder);
                typeNamesBuilder.Append(']');

                if (i != types.Length - 1) typeNamesBuilder.Append(',');
            }

            typeNamesBuilder.Append(']');
        }

        private static readonly Regex VersionRegex = new Regex(@", Version=\d+.\d+.\d+.\d+", RegexOptions.Compiled);
        private static readonly Regex CultureRegex = new Regex(@", Culture=\w+", RegexOptions.Compiled);
        private static readonly Regex PublicKeyTokenRegex = new Regex(@", PublicKeyToken=\w+", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<string, Type> TypeCache = new ConcurrentDictionary<string, Type>();

        internal static Type IgnoredAssemblyVersionTypeResolver(string typeName)
        {
            return TypeCache.GetOrAdd(typeName, value =>
            {
                value = VersionRegex.Replace(value, String.Empty);
                value = CultureRegex.Replace(value, String.Empty);
                value = PublicKeyTokenRegex.Replace(value, String.Empty);

                return DefaultTypeResolver(value);
            });
        }

        private static Type DefaultTypeResolver(string typeName)
        {
            typeName = typeName.Replace("System.Private.CoreLib", "mscorlib");
            return System.Type.GetType(typeName, throwOnError: true, ignoreCase: true);
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