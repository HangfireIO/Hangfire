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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Client;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire.Common
{
    /// <summary>
    /// Represents a class that contains the job filters.
    /// </summary>
    /// <remarks>
    /// Job filters run for every create, perform and state change
    /// of every job. All the instances in the filters collection
    /// should be thread-safe.
    /// 
    /// You can register a filter using the 
    /// <see cref="GlobalJobFilters.Filters"/> registration endpoint.
    /// </remarks>
    public class JobFilterCollection : IJobFilterProvider, IEnumerable<JobFilter>
    {
        private readonly List<JobFilter> _filters = new List<JobFilter>();

        /// <summary>
        /// Gets the number of filters in the global job filter collection.
        /// </summary>
        public int Count => _filters.Count;

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
        /// Determines whether a filter is in the global filter collection.
        /// </summary>
        /// <param name="filter">The filter instance.</param>
        /// <returns>True if the global filter collection contains the filter, otherwise false.</returns>
        public bool Contains(object filter)
        {
            return _filters.Any(x => x.Instance == filter);
        }

        /// <summary>
        /// Removes all filters that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter instance.</param>
        public void Remove(object filter)
        {
            _filters.RemoveAll(x => x.Instance == filter);
        }

        public IEnumerator<JobFilter> GetEnumerator()
        {
            return _filters.GetEnumerator();
        }

        IEnumerable<JobFilter> IJobFilterProvider.GetFilters(Job job)
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // ReSharper disable once UnusedParameter.Local
        private static void ValidateFilterInstance(object instance)
        {
            if (instance != null &&
                !(instance is IClientFilter 
                || instance is IServerFilter 
                || instance is IClientExceptionFilter 
                || instance is IServerExceptionFilter
                || instance is IApplyStateFilter
                || instance is IElectStateFilter))
            {
                throw new InvalidOperationException("Unsupported filter instance");
            }
        }
    }
}