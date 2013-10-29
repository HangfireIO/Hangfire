namespace HangFire.Filters
{
    /// <summary>
    /// Defines methods that are required for a client filter.
    /// </summary>
    public interface IClientFilter
    {
        /// <summary>
        /// Called before the creation of the job. 
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void OnCreating(CreatingContext filterContext);

        /// <summary>
        /// Called after the creation of the job.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void OnCreated(CreatedContext filterContext);
    }
}