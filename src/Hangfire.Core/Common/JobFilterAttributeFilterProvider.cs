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

using System.Collections.Generic;
using System.Linq;

namespace Hangfire.Common
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

        protected virtual IEnumerable<JobFilterAttribute> GetTypeAttributes(Job job)
        {
            return job.GetTypeFilterAttributes(_cacheAttributeInstances);
        }

        protected virtual IEnumerable<JobFilterAttribute> GetMethodAttributes(Job job)
        {
            return job.GetMethodFilterAttributes(_cacheAttributeInstances);
        }

        public virtual IEnumerable<JobFilter> GetFilters(Job job)
        {
            if (job == null) return Enumerable.Empty<JobFilter>();

            var typeFilters = GetTypeAttributes(job)
                .Select(attr => new JobFilter(attr, JobFilterScope.Type, null));

            var methodFilters = GetMethodAttributes(job)
                .Select(attr => new JobFilter(attr, JobFilterScope.Method, null));

            return typeFilters.Union(methodFilters).ToList();
        }
    }
}
