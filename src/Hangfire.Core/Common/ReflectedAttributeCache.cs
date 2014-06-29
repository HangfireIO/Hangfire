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
using System.Reflection;

namespace Hangfire.Common
{
    internal static class ReflectedAttributeCache
    {
        private static readonly ConcurrentDictionary<Type, ReadOnlyCollection<JobFilterAttribute>> TypeFilterAttributeCache 
            = new ConcurrentDictionary<Type, ReadOnlyCollection<JobFilterAttribute>>();

        private static readonly ConcurrentDictionary<MethodInfo, ReadOnlyCollection<JobFilterAttribute>> MethodFilterAttributeCache
            = new ConcurrentDictionary<MethodInfo, ReadOnlyCollection<JobFilterAttribute>>();

        public static ICollection<JobFilterAttribute> GetTypeFilterAttributes(Type type)
        {
            return GetAttributes(TypeFilterAttributeCache, type);
        }

        public static ICollection<JobFilterAttribute> GetMethodFilterAttributes(MethodInfo methodInfo)
        {
            return GetAttributes(MethodFilterAttributeCache, methodInfo);
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
    }
}
