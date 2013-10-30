using System.Collections.Generic;
using System.Linq;

namespace HangFire.Filters
{
    /// <summary>
    /// Defines a filter provider for filter attributes.
    /// </summary>
    public class JobFilterAttributeFilterProvider : IJobFilterProvider
    {
        private readonly bool _cacheAttributeInstances;

        /// <summary>
        /// Initializes a new instance of the  <see cref="JobFilterAttributeFilterProvider"/>
        /// class with the attribute instance caching enabled.
        /// </summary>
        public JobFilterAttributeFilterProvider()
            : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobFilterAttributeFilterProvider"/> 
        /// class and optionally caches attribute instances.
        /// </summary>
        /// <param name="cacheAttributeInstances"></param>
        public JobFilterAttributeFilterProvider(bool cacheAttributeInstances)
        {
            _cacheAttributeInstances = cacheAttributeInstances;
        }

        protected virtual IEnumerable<JobFilterAttribute> GetJobAttributes(JobDescriptor descriptor)
        {
            return descriptor.GetFilterAttributes(_cacheAttributeInstances);
        }

        public virtual IEnumerable<JobFilter> GetFilters(JobDescriptor descriptor)
        {
            var typeFilters = GetJobAttributes(descriptor)
                .Select(attr => new JobFilter(attr, JobFilterScope.Invoke, null));

            return typeFilters.ToList();
        }
    }
}
