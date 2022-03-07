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

namespace Hangfire.Common
{
    /// <summary>
    /// Represents a metadata class that contains a reference to the 
    /// implementation of one or more of the filter interfaces, the filter's 
    /// order, and the filter's scope.
    /// </summary>
    public class JobFilter
    {
        /// <summary>
        /// Represents a constant that is used to specify the default ordering of filters.
        /// </summary>
        public const int DefaultOrder = -1;

        /// <summary>
        /// Initializes a new instance of the Filter class.
        /// </summary>
        /// <param name="instance">Filter instance.</param>
        /// <param name="scope">Filter scope.</param>
        /// <param name="order">The run order.</param>
        public JobFilter(object instance, JobFilterScope scope, int? order)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (order == null)
            {
                var mvcFilter = instance as IJobFilter;
                if (mvcFilter != null)
                {
                    order = mvcFilter.Order;
                }
            }

            Instance = instance;
            Order = order ?? DefaultOrder;
            Scope = scope;
        }

        /// <summary>
        /// Gets the instance of the filter.
        /// </summary>
        public object Instance { get; protected set; }

        /// <summary>
        /// Gets the order in which the filter is applied.
        /// </summary>
        public int Order { get; protected set; }

        /// <summary>
        /// Gets the scope ordering of the filter.
        /// </summary>
        public JobFilterScope Scope { get; protected set; }
    }
}