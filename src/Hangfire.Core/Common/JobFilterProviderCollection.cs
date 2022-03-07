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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hangfire.Common
{
    /// <summary>
    /// Represents the collection of filter providers for the application.
    /// </summary>
    public class JobFilterProviderCollection : Collection<IJobFilterProvider>, IJobFilterProvider
    {
        // ReSharper disable once InconsistentNaming
        private static readonly FilterComparer _filterComparer = new FilterComparer();

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
            IEnumerable<JobFilter> combinedFilters =
                Items.SelectMany(fp => fp.GetFilters(job))
                    .OrderBy(filter => filter, _filterComparer);

            // Remove duplicates from the back forward
            return RemoveDuplicates(combinedFilters.Reverse()).Reverse();
        }

        private static IEnumerable<JobFilter> RemoveDuplicates(
            IEnumerable<JobFilter> filters)
        {
            var visitedTypes = new HashSet<Type>();

            foreach (JobFilter filter in filters)
            {
                var filterInstance = filter.Instance;
                var filterInstanceType = filterInstance.GetType();

                if (!visitedTypes.Contains(filterInstanceType) || AllowMultiple(filterInstance))
                {
                    yield return filter;
                    visitedTypes.Add(filterInstanceType);
                }
            }
        }

        private static bool AllowMultiple(object filterInstance)
        {
            var mvcFilter = filterInstance as IJobFilter;
            if (mvcFilter == null)
            {
                return true;
            }

            return mvcFilter.AllowMultiple;
        }

        private class FilterComparer : IComparer<JobFilter>
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
