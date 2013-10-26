namespace HangFire.Filters
{
    /// <summary>
    /// Defines methods that are required for a server filter.
    /// </summary>
    public interface IServerFilter : IJobFilter
    {
        /// <summary>
        /// Called before the performance of the job.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void OnPerforming(PerformingContext filterContext);

        /// <summary>
        /// Called after the performance of the job.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void OnPerformed(PerformedContext filterContext);
    }
}