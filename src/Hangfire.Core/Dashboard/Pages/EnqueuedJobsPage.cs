namespace Hangfire.Dashboard.Pages
{
    partial class EnqueuedJobsPage
    {
        public EnqueuedJobsPage(string queue)
        {
            Queue = queue;
        }

        public string Queue { get; }
    }
}
