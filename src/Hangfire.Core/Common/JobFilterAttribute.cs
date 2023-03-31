﻿// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

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

        [JsonIgnore]
        public bool AllowMultiple => AllowsMultiple(GetType());

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(JobFilter.DefaultOrder)]
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

#if !NETSTANDARD1_3
        [JsonIgnore]
        public override object TypeId => base.TypeId;
#endif

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
