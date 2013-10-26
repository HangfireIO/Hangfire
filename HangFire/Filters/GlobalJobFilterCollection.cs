using System.Collections;
using System.Collections.Generic;

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
    public class GlobalJobFilterCollection : IEnumerable<IJobFilter>
    {
        private readonly List<IJobFilter> _filters = new List<IJobFilter>();

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
        public void Add(IJobFilter filter)
        {
            _filters.Add(filter);
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
        public bool Contains(IJobFilter filter)
        {
            return _filters.Contains(filter);
        }

        /// <summary>
        /// Removes all filters that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter instance.</param>
        public void Remove(IJobFilter filter)
        {
            _filters.RemoveAll(x => x == filter);
        }

        public IEnumerator<IJobFilter> GetEnumerator()
        {
            return _filters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}