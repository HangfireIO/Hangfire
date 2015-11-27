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
using Hangfire.Common;
using Hangfire.Server;

namespace Hangfire.Storage
{
    public class InvocationData
    {
        public InvocationData(
            string type, string method, string parameterTypes, string arguments, string path)
        {
            Type = type;
            Method = method;
            ParameterTypes = parameterTypes;
            Arguments = arguments;
            Path = path;
        }

        public string Type { get; private set; }
        public string Method { get; private set; }
        public string ParameterTypes { get; private set; }
        public string Arguments { get; set; }
        public string Path { get; set; }

        public Job Deserialize()
        {
            try
            {                        
                var type = System.Type.GetType(Type, name => Assembly.LoadFrom(Path), null, true, true);
                var parameterTypes = JobHelper.FromJson<Type[]>(ParameterTypes);
                var method = GetNonOpenMatchingMethod(type, Method, parameterTypes);

                if (method == null)
                {
                    throw new InvalidOperationException(String.Format(
                        "The type `{0}` does not contain a method with signature `{1}({2})`",
                        type.FullName,
                        Method,
                        String.Join(", ", parameterTypes.Select(x => x.Name))));
                }

                var serializedArguments = JobHelper.FromJson<string[]>(Arguments);
                var arguments = DeserializeArguments(method, serializedArguments);

                return new Job(type, method, Path, arguments);
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
                JobHelper.ToJson(job.Method.GetParameters().Select(x => x.ParameterType).ToArray()),
                JobHelper.ToJson(SerializeArguments(job.Args)), 
                job.Path);
        }

        internal static string[] SerializeArguments(IReadOnlyCollection<object> arguments)
        {
            var serializedArguments = new List<string>(arguments.Count);
            foreach (var argument in arguments)
            {
                string value = null;

                if (argument != null)
                {
                    if (argument is DateTime)
                    {
                        value = ((DateTime)argument).ToString("o", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        value = JobHelper.ToJson(argument);
                    }
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
                    value = parameter.ParameterType.IsValueType
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
            catch (Exception jsonException)
            {
                if (type == typeof(object))
                {
                    // Special case for handling object types, because string can not
                    // be converted to object type.
                    value = argument;
                }
                else
                {
                    try
                    {
                        var converter = TypeDescriptor.GetConverter(type);
                        value = converter.ConvertFromInvariantString(argument);
                    }
                    catch (Exception)
                    {
                        throw jsonException;
                    }
                }
            }
            return value;
        }

        private static IEnumerable<MethodInfo> GetAllMethods(Type type)
        {
            var methods = new List<MethodInfo>(type.GetMethods());

            if (type.IsInterface)
            {
                methods.AddRange(type.GetInterfaces().SelectMany(x => x.GetMethods()));
            }

            return methods;
        }

        private static MethodInfo GetNonOpenMatchingMethod(Type type, string name, Type[] parameterTypes)
        {
            var methodCandidates = GetAllMethods(type);

            foreach (var methodCandidate in methodCandidates)
            {
                if (!methodCandidate.Name.Equals(name, StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = methodCandidate.GetParameters();
                if (parameters.Length != parameterTypes.Length)
                {
                    continue;
                }

                var parameterTypesMatched = true;
                var genericArguments = new List<Type>();

                // Determining whether we can use this method candidate with
                // current parameter types.
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var parameterType = parameter.ParameterType;
                    var actualType = parameterTypes[i];

                    // Skipping generic parameters as we can use actual type.
                    if (parameterType.IsGenericParameter)
                    {
                        genericArguments.Add(actualType);
                        continue;
                    }

                    // Skipping non-generic parameters of assignable types.
                    if (parameterType.IsAssignableFrom(actualType)) continue;

                    parameterTypesMatched = false;
                    break;
                }

                if (!parameterTypesMatched) continue;

                // Return first found method candidate with matching parameters.
                return methodCandidate.ContainsGenericParameters
                    ? methodCandidate.MakeGenericMethod(genericArguments.ToArray())
                    : methodCandidate;
            }

            return null;
        }
    }
}
