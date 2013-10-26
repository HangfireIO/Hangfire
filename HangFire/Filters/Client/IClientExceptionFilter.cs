namespace HangFire.Filters
{
    /// <summary>
    /// Defines methods that are required for the client exception filter.
    /// </summary>
    public interface IClientExceptionFilter : IJobFilter
    {
        /// <summary>
        /// Called when an exception occured during the creation of the job.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void OnClientException(ClientExceptionContext filterContext);
    }
}