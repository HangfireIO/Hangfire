using System;

namespace HangFire
{
    public class ClientFilterContext
    {
        internal ClientFilterContext(JobDescription jobDescription, Action enqueueAction)
        {
            JobDescription = jobDescription;
            EnqueueAction = enqueueAction;
        }

        public JobDescription JobDescription { get; private set; }
        public Action EnqueueAction { get; private set; }
    }
}