// This file is part of Hangfire. Copyright © 2014 Hangfire OÜ.
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Hangfire.Common
{
    internal static class TypeExtensions
    {
        private static readonly Regex GenericArgumentsRegex = new Regex(@"`[1-9]\d*", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        public static string ToGenericTypeString(this Type type)
        {
            if (!type.GetTypeInfo().IsGenericType)
            {
                return type.GetFullNameWithoutNamespace()
                        .ReplacePlusWithDotInNestedTypeName();
            }

            return type.GetGenericTypeDefinition()
                    .GetFullNameWithoutNamespace()
                    .ReplacePlusWithDotInNestedTypeName()
                    .ReplaceGenericParametersInGenericTypeName(type);
        }

        public static MethodInfo? GetNonOpenMatchingMethod(
            this Type type,
            string name,
            Type[]? parameterTypes)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (name == null) throw new ArgumentNullException(nameof(name));

            parameterTypes = parameterTypes ?? Type.EmptyTypes;

            var methodCandidates = new List<MethodInfo>(type.GetRuntimeMethods());

            if (type.GetTypeInfo().IsInterface)
            {
                methodCandidates.AddRange(type.GetTypeInfo()
                    .ImplementedInterfaces.SelectMany(static x => x.GetRuntimeMethods()));
            }

            foreach (var methodCandidate in methodCandidates)
            {
                if (!methodCandidate.GetNormalizedName().Equals(name, StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = methodCandidate.GetParameters();
                if (parameters.Length != parameterTypes.Length)
                {
                    continue;
                }

                var parameterTypesMatched = true;

                var genericArguments = methodCandidate.ContainsGenericParameters
                    ? new Type[methodCandidate.GetGenericArguments().Length]
                    : null;
                
                // Determining whether we can use this method candidate with
                // current parameter types.
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameterType = parameters[i].ParameterType.GetTypeInfo();
                    var actualType = parameterTypes[i].GetTypeInfo();

                    if (!TypesMatchRecursive(parameterType, actualType, genericArguments))
                    {
                        parameterTypesMatched = false;
                        break;
                    }
                }

                if (parameterTypesMatched)
                {
                    if (genericArguments != null)
                    {
                        var genericArgumentsResolved = true;

                        foreach (var genericArgument in genericArguments)
                        {
                            if (genericArgument == null)
                            {
                                genericArgumentsResolved = false;
                            }
                        }

                        if (genericArgumentsResolved)
                        {
                            return methodCandidate.MakeGenericMethod(genericArguments);
                        }
                    }
                    else
                    {
                        // Return first found method candidate with matching parameters.
                        return methodCandidate;
                    }
                }
            }

            return null;
        }

        public static Type[] GetAllGenericArguments(this TypeInfo type)
        {
            return type.GenericTypeArguments.Length > 0 ? type.GenericTypeArguments : type.GenericTypeParameters;
        }

        private static bool TypesMatchRecursive(TypeInfo parameterType, TypeInfo actualType, IList<Type?>? genericArguments)
        {
            if (parameterType.IsGenericParameter)
            {
                if (genericArguments == null)
                {
                    throw new ArgumentException($"Type '{parameterType.FullName}' is generic parameter, but no generic arguments list was provided.'", nameof(genericArguments));
                }

                var position = parameterType.GenericParameterPosition;
                var genericArgument = genericArguments[position];
                
                // Return false if this generic parameter has been identified and it's not the same as actual type
                if (genericArgument != null && genericArgument.GetTypeInfo() != actualType)
                {
                    return false;
                }

                genericArguments[position] = actualType.AsType();
                return true;
            }

            if (parameterType.ContainsGenericParameters)
            {
                if (parameterType.IsArray)
                {
                    // Return false if parameterType is array whereas actualType isn't
                    if (!actualType.IsArray) return false;

                    var parameterElementType = parameterType.GetElementType()!;
                    var actualElementType = actualType.GetElementType()!;

                    return TypesMatchRecursive(parameterElementType.GetTypeInfo(), actualElementType.GetTypeInfo(), genericArguments);
                }

                if (!actualType.IsGenericType || parameterType.GetGenericTypeDefinition() != actualType.GetGenericTypeDefinition())
                {
                    return false;
                }

                for (var i = 0; i < parameterType.GenericTypeArguments.Length; i++)
                {
                    var parameterGenericArgument = parameterType.GenericTypeArguments[i];
                    var actualGenericArgument = actualType.GenericTypeArguments[i];

                    if (!TypesMatchRecursive(parameterGenericArgument.GetTypeInfo(), actualGenericArgument.GetTypeInfo(), genericArguments))
                    {
                        return false;
                    }
                }

                return true;
            }

            return parameterType != typeof(object).GetTypeInfo() 
                ? parameterType.IsAssignableFrom(actualType)
                : parameterType == actualType;
        }

        private static string GetFullNameWithoutNamespace(this Type type)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            var fullName = type.FullName;
            if (fullName == null)
            {
                throw new NotSupportedException($"Type '{type.Name}' does not have a full name.");
            }

            const int dotLength = 1;
            // ReSharper disable once PossibleNullReferenceException
            return !String.IsNullOrEmpty(type.Namespace)
                ? fullName.Substring(type.Namespace.Length + dotLength)
                : fullName;
        }

        private static string ReplacePlusWithDotInNestedTypeName(this string typeName)
        {
            return typeName.Replace('+', '.');
        }

        private static string ReplaceGenericParametersInGenericTypeName(this string typeName, Type type)
        {
            var genericArguments = type .GetTypeInfo().GetAllGenericArguments();

            typeName = GenericArgumentsRegex.Replace(typeName, match =>
            {
                var currentGenericArgumentNumbers = int.Parse(match.Value.Substring(1), CultureInfo.InvariantCulture);
                var currentArguments = string.Join(",", genericArguments.Take(currentGenericArgumentNumbers).Select(ToGenericTypeString));
                genericArguments = genericArguments.Skip(currentGenericArgumentNumbers).ToArray();
                return string.Concat("<", currentArguments, ">");
            });

            return typeName;
        }
    }
}
