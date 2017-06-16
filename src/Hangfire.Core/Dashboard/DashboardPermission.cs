namespace Hangfire.Dashboard
{
    public enum DashboardPermission
    {
        /// <summary>
        /// User has permission to delete tasks.
        /// </summary>
        DeleteJob,

        /// <summary>
        /// User has permission to enqueue/trigger tasks.
        /// </summary>
        EnqueueJob
    };
}