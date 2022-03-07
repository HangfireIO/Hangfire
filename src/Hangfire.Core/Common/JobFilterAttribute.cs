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
using System.Linq;
using System.Reflection;

namespace Hangfire.Common
{
    /// <summary>
    /// Represents the base class for job filter attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
    public abstract class JobFilterAttribute : Attribute, IJobFilter
    {
        private static readonly ConcurrentDictionary<Type, bool> MultiuseAttributeCache = new ConcurrentDictionary<Type, bool>();
        private int _order = JobFilter.DefaultOrder;

        public bool AllowMultiple => AllowsMultiple(GetType());

        public int Order
        {
            get { return _order; }
            set
            {
                if (value < JobFilter.DefaultOrder)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The Order value should be greater or equal to '-1'");
                }
                _order = value;
            }
        }

        private static bool AllowsMultiple(Type attributeType)
        {
            return MultiuseAttributeCache.GetOrAdd(
                attributeType,
                type => type.GetTypeInfo()
                            .GetCustomAttributes(typeof(AttributeUsageAttribute), true)
                            .Cast<AttributeUsageAttribute>()
                            .First()
                            .AllowMultiple);
        }
    }
}
