using System.Collections.Generic;

namespace HangFire.Filters
{
    /// <summary>
    /// Provides an interface for finding filters.
    /// </summary>
    public interface IJobFilterProvider
    {
        /// <summary>
        /// Returns an enumerator that contains all the <see cref="IJobFilterProvider"/>.
        /// </summary>
        /// 
        /// <param name="descriptor">Job descriptor, can be null.</param>
        /// 
        /// <returns>
        /// The enumerator that contains all the <see cref="IJobFilterProvider"/>.
        /// </returns>
        IEnumerable<JobFilter> GetFilters(JobDescriptor descriptor);
    }
}
