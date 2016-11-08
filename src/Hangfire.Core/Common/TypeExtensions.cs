// This file is part of Hangfire.
// Copyright © 2014 Sergey Odinokov.
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
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Hangfire.Annotations;

namespace Hangfire.Common
{
    internal static class TypeExtensions
    {
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

        public static MethodInfo GetNonOpenMatchingMethod(
            [NotNull] this Type type,
            [NotNull] string name,
            [CanBeNull] Type[] parameterTypes)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (name == null) throw new ArgumentNullException(nameof(name));

            parameterTypes = parameterTypes ?? new Type[0];

            var methodCandidates = new List<MethodInfo>(type.GetRuntimeMethods());

            if (type.GetTypeInfo().IsInterface)
            {
                methodCandidates.AddRange(type.GetTypeInfo().ImplementedInterfaces.SelectMany(x => x.GetRuntimeMethods()));
            }

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

                var methodGenericArguments = methodCandidate.GetGenericArguments().ToDictionary(arg => arg, arg => (Type) null);

                // Determining whether we can use this method candidate with
                // current parameter types.
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var parameterType = parameter.ParameterType;
                    var actualType = parameterTypes[i];

                    if (parameterType.GetTypeInfo().ContainsGenericParameters)
                    {
                        // Skipping generic parameters as we can use actual type.
                        parameterTypesMatched = parameterType.TryGetGenericArguments(actualType, ref methodGenericArguments);

                        if (!parameterTypesMatched) break;

                        continue;
                    }

                    // Skipping non-generic parameters of equal types.
                    if (parameterType.GetTypeInfo().Equals(actualType.GetTypeInfo())) continue;

                    parameterTypesMatched = false;
                    break;
                }

                if (!parameterTypesMatched) continue;

                // Return first found method candidate with matching parameters.
                return methodCandidate.ContainsGenericParameters
                    ? methodCandidate.MakeGenericMethod(methodGenericArguments.Values.ToArray())
                    : methodCandidate;
            }

            return null;
        }

        public static Type[] GetAllGenericArguments(this TypeInfo type)
        {
            return type.GenericTypeArguments.Length > 0 ? type.GenericTypeArguments : type.GenericTypeParameters;
        }

        private static bool TryGetGenericArguments(this Type type, Type actualType, ref Dictionary<Type, Type> methodGenricArguments)
        {
            var typeInfo = type.GetTypeInfo();
            var actualTypeInfo = actualType.GetTypeInfo();

            if (!IsTypeMatched(typeInfo, actualTypeInfo)) return false;

            if (!typeInfo.ContainsGenericParameters) return true;

            if (typeInfo.IsGenericParameter)
            {
                //Return false if this generic parameter has been identified and it's not the same as actual type
                if (methodGenricArguments[type] != null && methodGenricArguments[type] != actualType)
                {
                    return false;
                }
                methodGenricArguments[type] = actualType;
            }

            if (typeInfo.IsGenericType && typeInfo.ContainsGenericParameters)
            {
                for (var i = 0; i < typeInfo.GenericTypeArguments.Length; i++)
                {
                    var genericTypeArgument = typeInfo.GenericTypeArguments[i];
                    var actualGenericArgument = actualTypeInfo.GenericTypeArguments[i];

                    var typeMatched = genericTypeArgument.TryGetGenericArguments(actualGenericArgument, ref methodGenricArguments);

                    if (!typeMatched) return false;
                }
            }

            return true;
        }

        private static bool IsTypeMatched(this TypeInfo genericParameterType, TypeInfo actualType)
        {
            if (genericParameterType.IsGenericParameter)
            {
                return true;
            }

            if (genericParameterType.IsGenericType != actualType.IsGenericType)
            {
                return false;
            }

            if (genericParameterType.IsGenericType && genericParameterType.ContainsGenericParameters)
            {
                return genericParameterType.GetGenericTypeDefinition().GetTypeInfo()
                    .Equals(actualType.GetGenericTypeDefinition().GetTypeInfo());
            }

            return genericParameterType.Equals(actualType);
        }

        private static string GetFullNameWithoutNamespace(this Type type)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            const int dotLength = 1;
            // ReSharper disable once PossibleNullReferenceException
            return !String.IsNullOrEmpty(type.Namespace)
                ? type.FullName.Substring(type.Namespace.Length + dotLength)
                : type.FullName;
        }

        private static string ReplacePlusWithDotInNestedTypeName(this string typeName)
        {
            return typeName.Replace('+', '.');
        }

        private static string ReplaceGenericParametersInGenericTypeName(this string typeName, Type type)
        {
            var genericArguments = type .GetTypeInfo().GetAllGenericArguments();

            const string regexForGenericArguments = @"`[1-9]\d*";

            var rgx = new Regex(regexForGenericArguments);

            typeName = rgx.Replace(typeName, match =>
            {
                var currentGenericArgumentNumbers = int.Parse(match.Value.Substring(1));
                var currentArguments = string.Join(",", genericArguments.Take(currentGenericArgumentNumbers).Select(ToGenericTypeString));
                genericArguments = genericArguments.Skip(currentGenericArgumentNumbers).ToArray();
                return string.Concat("<", currentArguments, ">");
            });

            return typeName;
        }
    }
}
