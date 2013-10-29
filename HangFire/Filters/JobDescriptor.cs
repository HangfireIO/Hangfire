using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire.Filters
{
    /// <summary>
    /// Represents the base class for job descriptors.
    /// </summary>
    public abstract class JobDescriptor
    {
        /// <summary>
        /// Gets the state of the creating job.
        /// </summary>
        public string JobId { get; protected set; }

        /// <summary>
        /// Gets the type of the creating job.
        /// </summary>
        public Type Type { get; protected set; }

        internal IEnumerable<JobFilterAttribute> GetFilterAttributes(bool useCache)
        {
            if (useCache)
            {
                return ReflectedAttributeCache.GetTypeFilterAttributes(Type);
            }

            return Type
                .GetCustomAttributes(typeof(JobFilterAttribute), inherit: true)
                .Cast<JobFilterAttribute>();
        }
    }
}
