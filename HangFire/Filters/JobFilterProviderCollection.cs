using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HangFire.Filters
{
    /// <summary>
    /// Represents the collection of filter providers for the application.
    /// </summary>
    public class JobFilterProviderCollection : Collection<IJobFilterProvider>
    {
        private static readonly FilterComparer _filterComparer = new FilterComparer();

        /// <summary>
        /// Initializes a new instance of the <see cref="JobFilterProviderCollection"/> 
        /// class.
        /// </summary>
        public JobFilterProviderCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobFilterProviderCollection"/> 
        /// class using the filter providers collection.
        /// </summary>
        /// <param name="providers"></param>
        public JobFilterProviderCollection(IList<IJobFilterProvider> providers)
            : base(providers)
        {
        }

        internal JobFilterProviderCollection(params IJobFilterProvider[] providers)
            : base(providers)
        {
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

        /// <summary>
        /// Returns the collection of filter providers.
        /// </summary>
        /// <param name="descriptor">Job descriptor, can be null.</param>
        /// <returns>The collection of filter providers.</returns>
        public IEnumerable<JobFilter> GetFilters(JobDescriptor descriptor)
        {
            IEnumerable<JobFilter> combinedFilters =
                Items.SelectMany(fp => fp.GetFilters(descriptor))
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
