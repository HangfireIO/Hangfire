using System;
using System.Collections.Concurrent;
using System.Linq;

namespace HangFire.Filters
{
    /// <summary>
    /// Represents the base class for job filter attributes.
    /// </summary>
    [AttributeUsageAttribute(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
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
                    throw new ArgumentOutOfRangeException("value", "The Order value should be more that -1");
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
