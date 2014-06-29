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
