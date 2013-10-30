using System;

namespace HangFire.Web.Pages
{
    partial class JobDetailsPage
    {
        public JobDetailsPage(string jobId)
        {
            JobId = Guid.Parse(jobId);
        }

        public Guid JobId { get; private set; }
    }
}
