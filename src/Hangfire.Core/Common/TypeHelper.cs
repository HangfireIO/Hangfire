// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Hangfire.Common
{
    public class TypeHelper
    {
        private static readonly ConcurrentDictionary<Type, string> TypeSerializerCache = new ConcurrentDictionary<Type, string>();

        private static readonly Assembly CoreLibrary = typeof(int).GetTypeInfo().Assembly;
        private static readonly AssemblyName MscorlibAssemblyName = new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        private static readonly ConcurrentDictionary<string, Assembly> AssemblyCache = new ConcurrentDictionary<string, Assembly>();
        private static readonly ConcurrentDictionary<string, Type> TypeResolverCache = new ConcurrentDictionary<string, Type>();

        private static readonly Regex VersionRegex = new Regex(@", Version=\d+.\d+.\d+.\d+", RegexOptions.Compiled);
        private static readonly Regex CultureRegex = new Regex(@", Culture=\w+", RegexOptions.Compiled);
        private static readonly Regex PublicKeyTokenRegex = new Regex(@", PublicKeyToken=\w+", RegexOptions.Compiled);

        private static Func<string, Type> _currentTypeResolver;
        private static Func<Type, string> _currentTypeSerializer;

        public static Func<string, Type> CurrentTypeResolver
        {
            get => Volatile.Read(ref _currentTypeResolver) ?? DefaultTypeResolver;
            set => Volatile.Write(ref _currentTypeResolver, value);
        }

        public static Func<Type, string> CurrentTypeSerializer
        {
            get => Volatile.Read(ref _currentTypeSerializer) ?? DefaultTypeSerializer;
            set => Volatile.Write(ref _currentTypeSerializer, value);
        }

        public static string DefaultTypeSerializer(Type type)
        {
            return type.AssemblyQualifiedName;
        }

        public static string SimpleAssemblyTypeSerializer(Type type)
        {
            return TypeSerializerCache.GetOrAdd(type, value =>
            {
                var builder = new StringBuilder();
                SerializeType(value, true, builder);

                return builder.ToString();
            });
        }

        public static Type DefaultTypeResolver(string typeName)
        {
#if NETSTANDARD1_3
            typeName = typeName.Replace("System.Private.CoreLib", "mscorlib");
            return Type.GetType(
                typeName,
                throwOnError: true);
#else
            return Type.GetType(
                typeName,
                typeResolver: TypeResolver,
                assemblyResolver: CachedAssemblyResolver,
                throwOnError: true);
#endif
        }

        public static Type IgnoredAssemblyVersionTypeResolver(string typeName)
        {
            return TypeResolverCache.GetOrAdd(typeName, value =>
            {
                value = VersionRegex.Replace(value, String.Empty);
                value = CultureRegex.Replace(value, String.Empty);
                value = PublicKeyTokenRegex.Replace(value, String.Empty);

                return DefaultTypeResolver(value);
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

        private static Assembly CachedAssemblyResolver(AssemblyName assemblyName)
        {
            return AssemblyCache.GetOrAdd(assemblyName.FullName, AssemblyResolver);
        }

        private static Assembly AssemblyResolver(string assemblyString)
        {
            var assemblyName = new AssemblyName(assemblyString);

            if (assemblyName.Name.Equals("System.Console", StringComparison.OrdinalIgnoreCase) ||
                assemblyName.Name.Equals("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) ||
                assemblyName.Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = MscorlibAssemblyName;
            }

            var publicKeyToken = assemblyName.GetPublicKeyToken();

#if !NETSTANDARD1_3
            if (assemblyName.Version == null && assemblyName.CultureInfo == null && publicKeyToken == null)
            {
#pragma warning disable 618
                return Assembly.LoadWithPartialName(assemblyName.Name);
#pragma warning restore 618
            }
#endif

            try
            {
                return Assembly.Load(assemblyName);
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                var shortName = new AssemblyName(assemblyName.Name);
                if (publicKeyToken != null)
                {
                    shortName.SetPublicKeyToken(publicKeyToken);
                }

                return Assembly.Load(shortName);
            }
        }

        private static Type TypeResolver(Assembly assembly, string typeName, bool ignoreCase)
        {
            if (typeName.Equals("System.Diagnostics.Debug", StringComparison.Ordinal))
            {
                return typeof(System.Diagnostics.Debug);
            }

            assembly = assembly ?? CoreLibrary;

            if (assembly != CoreLibrary &&
                assembly.GetName().Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
            {
                // Everything defaults to `mscorlib` for interoperability reasons between
                // .NET Framework and .NET Core. Most of the types have the proper forwarding,
                // but newer types like DateOnly or TimeOnly don't. So for types from `mscorlib`
                // we perform the first search in the current core library.
                var type = CoreLibrary.GetType(typeName, false, ignoreCase);
                if (type != null) return type;
            }

            return assembly.GetType(typeName, true, ignoreCase);
        }
    }
}
