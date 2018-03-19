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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Hangfire.Common
{
    internal static class ReflectedAttributeCache
    {
        private static readonly ConcurrentDictionary<TypeInfo, ReadOnlyCollection<JobFilterAttribute>> TypeFilterAttributeCache 
            = new ConcurrentDictionary<TypeInfo, ReadOnlyCollection<JobFilterAttribute>>();

        private static readonly ConcurrentDictionary<MethodInfo, ReadOnlyCollection<JobFilterAttribute>> MethodFilterAttributeCache
            = new ConcurrentDictionary<MethodInfo, ReadOnlyCollection<JobFilterAttribute>>();

        private static readonly ConcurrentDictionary<TypeInfo, QueueAttribute> TypeQueueAttributeCache
            = new ConcurrentDictionary<TypeInfo, QueueAttribute>();

        private static readonly ConcurrentDictionary<MethodInfo, QueueAttribute> MethodQueueAttributeCache
            = new ConcurrentDictionary<MethodInfo, QueueAttribute>();

        public static ICollection<JobFilterAttribute> GetTypeFilterAttributes(Type type)
        {
            return GetAttributes(TypeFilterAttributeCache, type.GetTypeInfo());
        }

        public static ICollection<JobFilterAttribute> GetMethodFilterAttributes(MethodInfo methodInfo)
        {
            return GetAttributes(MethodFilterAttributeCache, methodInfo);
        }

        public static QueueAttribute GetTypeQueueAttribute(Type type)
        {
            return GetAttibute(TypeQueueAttributeCache, type.GetTypeInfo());
        }

        public static QueueAttribute GetMethodQueueAttribute(MethodInfo methodInfo)
        {
            return GetAttibute(MethodQueueAttributeCache, methodInfo);
        }

        private static ReadOnlyCollection<TAttribute> GetAttributes<TMemberInfo, TAttribute>(
            ConcurrentDictionary<TMemberInfo, ReadOnlyCollection<TAttribute>> lookup, 
            TMemberInfo memberInfo)
            where TAttribute : Attribute
            where TMemberInfo : MemberInfo
        {
            Debug.Assert(memberInfo != null);
            Debug.Assert(lookup != null);

            return lookup.GetOrAdd(memberInfo, mi => new ReadOnlyCollection<TAttribute>((TAttribute[])memberInfo.GetCustomAttributes(typeof(TAttribute), inherit: true)));
        }

        private static TAttribute GetAttibute<TMemberInfo, TAttribute>(
            ConcurrentDictionary<TMemberInfo, TAttribute> lookup,
            TMemberInfo memberInfo)
            where TAttribute : Attribute
            where TMemberInfo : MemberInfo
        {
            Debug.Assert(memberInfo != null);
            Debug.Assert(lookup != null);

            return lookup.GetOrAdd(memberInfo, mi => memberInfo.GetCustomAttributes(typeof(TAttribute), inherit: true).Cast<TAttribute>().SingleOrDefault());
        }
    }
}
