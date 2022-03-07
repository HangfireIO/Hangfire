// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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
        private static readonly ConcurrentDictionary<TypeInfo, ReadOnlyCollection<JobFilterAttribute>> TypeFilterAttributeCache 
            = new ConcurrentDictionary<TypeInfo, ReadOnlyCollection<JobFilterAttribute>>();

        private static readonly ConcurrentDictionary<MethodInfo, ReadOnlyCollection<JobFilterAttribute>> MethodFilterAttributeCache
            = new ConcurrentDictionary<MethodInfo, ReadOnlyCollection<JobFilterAttribute>>();

        public static ICollection<JobFilterAttribute> GetTypeFilterAttributes(Type type)
        {
            return GetAttributes(TypeFilterAttributeCache, type.GetTypeInfo());
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
