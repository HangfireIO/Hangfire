using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace HangFire.Filters
{
    internal static class ReflectedAttributeCache
    {
        private static readonly ConcurrentDictionary<Type, ReadOnlyCollection<JobFilterAttribute>> TypeFilterAttributeCache 
            = new ConcurrentDictionary<Type, ReadOnlyCollection<JobFilterAttribute>>();

        public static ICollection<JobFilterAttribute> GetTypeFilterAttributes(Type type)
        {
            return GetAttributes(TypeFilterAttributeCache, type);
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
