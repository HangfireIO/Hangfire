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

        private struct Continuation
        {
            public string JobId { get; [UsedImplicitly] set; }
            public JobContinuationOptions Options { get; [UsedImplicitly] set; }
        }
    }
}
