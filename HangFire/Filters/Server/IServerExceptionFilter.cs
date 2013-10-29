namespace HangFire.Filters
{
    /// <summary>
    /// Defines methods that are required for the server exception filter.
    /// </summary>
    public interface IServerExceptionFilter
    {
        /// <summary>
        /// Called when an exception occured during the performance of the job.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void OnServerException(ServerExceptionContext filterContext);
    }
}
