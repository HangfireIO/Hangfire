using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HangFire.Filters
{
    /// <summary>
    /// Represents a class that contains all the global job filters.
    /// </summary>
    /// <remarks>
    /// Global filters run for every create, perform and state change
    /// of every job. All the instances in the global filters collection
    /// must be thread-safe.
    /// 
    /// You can register a global filter using the 
    /// <see cref="GlobalJobFilters.Filters"/> registration endpoint.
    /// </remarks>
    public class GlobalJobFilterCollection : IJobFilterProvider, IEnumerable<JobFilter>
    {
        private readonly List<JobFilter> _filters = new List<JobFilter>();

        /// <summary>
        /// Gets the number of filters in the global job filter collection.
        /// </summary>
        public int Count
        {
            get
            {
                return _filters.Count;
            }
        }

        /// <summary>
        /// Adds the specified filter to the global filter collection.
        /// </summary>
        /// <param name="filter">The filter instance.</param>
        public void Add(object filter)
        {
            AddInternal(filter, order: null);
        }

        /// <summary>
        /// Adds the specified filter to the global filter collection 
        /// using the specified filter run order.
        /// </summary>
        /// <param name="filter">The filter instance.</param>
        /// <param name="order">The run order.</param>
        public void Add(object filter, int order)
        {
            AddInternal(filter, order);
        }

        private void AddInternal(object filter, int? order)
        {
            ValidateFilterInstance(filter);
            _filters.Add(new JobFilter(filter, JobFilterScope.Global, order));
        }

        /// <summary>
        /// Removes all filters from the global filter collection.
        /// </summary>
        public void Clear()
        {
            _filters.Clear();
        }

        /// <summary>
        /// Determines wheither a filter is in the global filter collection.
        /// </summary>
        /// <param name="filter">The filter instance.</param>
        /// <returns>True if the global filter collection contains the filter, otherwise false.</returns>
        public bool Contains(object filter)
        {
            return _filters.Any(x => x == filter);
        }

        /// <summary>
        /// Removes all filters that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter instance.</param>
        public void Remove(object filter)
        {
            _filters.RemoveAll(x => x == filter);
        }

        public IEnumerator<JobFilter> GetEnumerator()
        {
            return _filters.GetEnumerator();
        }

        IEnumerable<JobFilter> IJobFilterProvider.GetFilters(JobDescriptor descriptor)
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static void ValidateFilterInstance(object instance)
        {
            if (instance != null &&
                !(instance is IClientFilter 
                || instance is IServerFilter 
                || instance is IClientExceptionFilter 
                || instance is IServerExceptionFilter
                || instance is IStateChangedFilter
                || instance is IStateChangingFilter))
            {
                throw new InvalidOperationException("Unsupported filter instance");
            }
        }
    }
}