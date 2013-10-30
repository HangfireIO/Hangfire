using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Filters;

namespace HangFire
{
    /// <summary>
    /// Represents the base class for job descriptors.
    /// </summary>
    public class JobDescriptor
    {
        internal JobDescriptor(string jobId, Type jobType)
        {
            JobId = jobId;
            Type = jobType;
        }

        internal JobDescriptor(string jobId, string jobType)
        {
            JobId = jobId;

            try
            {
                Type = Type.GetType(jobType, throwOnError: true);
            }
            catch (Exception ex)
            {
                TypeLoadException = ex;
            }
        }

        /// <summary>
        /// Gets the state of the creating job.
        /// </summary>
        public string JobId { get; private set; }

        /// <summary>
        /// Gets the type of the creating job.
        /// </summary>
        public Type Type { get; private set; }

        public Exception TypeLoadException { get; private set; }

        internal IEnumerable<JobFilterAttribute> GetFilterAttributes(bool useCache)
        {
            if (Type == null)
            {
                return Enumerable.Empty<JobFilterAttribute>();
            }

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
