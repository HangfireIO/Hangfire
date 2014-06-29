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
                throw new ArgumentNullException("instance");
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