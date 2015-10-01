using System;
using System.Linq;
using System.Reflection;

namespace Hangfire.Common
{
    internal static class TypeExtensions
    {
        public static string ToGenericTypeString(this Type t)
        {
            if (!t.GetTypeInfo().IsGenericType)
            {
                return t.Name;
            }

            var genericTypeName = t.GetGenericTypeDefinition().Name;
            genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));

            var genericArgs = string.Join(",", t.GetGenericArguments().Select(ToGenericTypeString).ToArray());

            return genericTypeName + "<" + genericArgs + ">";
        }
    }
}
