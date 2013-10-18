namespace HangFire.Web.Pages
{
    partial class DequeuedJobsPage
    {
        public DequeuedJobsPage(string queue)
        {
            Queue = queue;
        }

        public string Queue { get; private set; }
    }
}
