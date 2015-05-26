﻿using System;
using System.Linq;

namespace Hangfire.Common
{
    public static class TypeExtensions
    {
        public static string ToGenericTypeString(this Type t)
        {
            if (!t.IsGenericType)
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
