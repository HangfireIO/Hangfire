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
using System.Linq;

namespace Hangfire.Common
{
    /// <summary>
    /// Represents the base class for job filter attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public abstract class JobFilterAttribute : Attribute, IJobFilter
    {
        private static readonly ConcurrentDictionary<Type, bool> MultiuseAttributeCache = new ConcurrentDictionary<Type, bool>();
        private int _order = JobFilter.DefaultOrder;

        public bool AllowMultiple
        {
            get { return AllowsMultiple(GetType()); }
        }

        public int Order
        {
            get { return _order; }
            set
            {
                if (value < JobFilter.DefaultOrder)
                {
                    throw new ArgumentOutOfRangeException("value", "The Order value should be greater or equal to '-1'");
                }
                _order = value;
            }
        }

        private static bool AllowsMultiple(Type attributeType)
        {
            return MultiuseAttributeCache.GetOrAdd(
                attributeType,
                type => type.GetCustomAttributes(typeof(AttributeUsageAttribute), true)
                            .Cast<AttributeUsageAttribute>()
                            .First()
                            .AllowMultiple);
        }
    }
}
