using Hangfire.Annotations;

namespace Hangfire.Dashboard.Pages
{
    partial class JobDetailsPage
    {
        public JobDetailsPage(string jobId)
        {
            JobId = jobId;
        }

        public string JobId { get; }

    }
}
