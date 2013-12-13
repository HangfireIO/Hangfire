// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using HangFire.Client;

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

        protected virtual IEnumerable<JobFilterAttribute> GetJobAttributes(
            JobMethod method)
        {
            return method.GetTypeFilterAttributes(_cacheAttributeInstances);
        }

        public virtual IEnumerable<JobFilter> GetFilters(JobMethod method)
        {
            var typeFilters = GetJobAttributes(method)
                .Select(attr => new JobFilter(attr, JobFilterScope.Type, null));

            return typeFilters.ToList();
        }
    }
}
