using System;

namespace HangFire.Web.Pages
{
    partial class JobDetailsPage
    {
        public JobDetailsPage(string jobId)
        {
            // TODO: throw 400 exception
            JobId = Guid.Parse(jobId);
        }

        public Guid JobId { get; private set; }
    }
}
