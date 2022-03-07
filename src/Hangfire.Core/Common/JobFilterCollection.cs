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