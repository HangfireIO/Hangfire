// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Hangfire.Common
{
    /// <summary>
    /// Represents the collection of filter providers for the application.
    /// </summary>
    public class JobFilterProviderCollection : Collection<IJobFilterProvider>, IJobFilterProvider
    {
        private static readonly JobFilterComparer FilterComparer = new JobFilterComparer();

        /// <summary>
        /// Initializes a new instance of the <see cref="JobFilterProviderCollection"/> 
        /// class.
        /// </summary>
        public JobFilterProviderCollection()
        {
        }

        public JobFilterProviderCollection(params IJobFilterProvider[] providers)
            : base(providers)
        {
        }

        /// <summary>
        /// Returns the collection of filter providers.
        /// </summary>
        /// <param name="job">Job, can be null.</param>
        /// <returns>The collection of filter providers.</returns>
        public IEnumerable<JobFilter> GetFilters(Job job)
        {
            var combinedFilters = new List<JobFilter>();

            for (var i = 0; i < Items.Count; i++)
            {
                combinedFilters.AddRange(Items[i].GetFilters(job));
            }

            if (combinedFilters.Count > 1)
            {
                // Sorting before removing duplicates in the correct order
                combinedFilters = combinedFilters.OrderBy(static filter => filter, FilterComparer).ToList();
                RemoveDuplicates(combinedFilters);
            }

            return combinedFilters;
        }

        private static void RemoveDuplicates(List<JobFilter> filters)
        {
            var visitedTypes = new HashSet<Type>();

            // Remove duplicates from the back forward
            for (var i = filters.Count - 1; i >= 0; i--)
            {
                var filterInstance = filters[i].Instance;

                if (!visitedTypes.Add(filterInstance.GetType()) && !AllowMultiple(filterInstance))
                {
                    filters.RemoveAt(i);
                }
            }
        }

        private static bool AllowMultiple(object filterInstance)
        {
            if (filterInstance is IJobFilter jobFilter)
            {
                return jobFilter.AllowMultiple;
            }

            return true;
        }

        private sealed class JobFilterComparer : IComparer<JobFilter>
        {
            public int Compare(JobFilter x, JobFilter y)
            {
                // Nulls always have to be less than non-nulls
                if (x == null && y == null)
                {
                    return 0;
                }
                if (x == null)
                {
                    return -1;
                }
                if (y == null)
                {
                    return 1;
                }

                // Sort first by order...

                if (x.Order < y.Order)
                {
                    return -1;
                }
                if (x.Order > y.Order)
                {
                    return 1;
                }

                // ...then by scope

                if (x.Scope < y.Scope)
                {
                    return -1;
                }
                if (x.Scope > y.Scope)
                {
                    return 1;
                }

                return 0;
            }
        }
    }
}