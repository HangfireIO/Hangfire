using System;

namespace HangFire.Filters
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