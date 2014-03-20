namespace HangFire.Web.Pages
{
    partial class FetchedJobsPage
    {
        public FetchedJobsPage(string queue)
        {
            Queue = queue;
        }

        public string Queue { get; private set; }
    }
}
