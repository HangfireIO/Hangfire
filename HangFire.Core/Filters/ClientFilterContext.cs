using System;
using System.Collections.Generic;

namespace HangFire
{
    public class ClientFilterContext
    {
        internal ClientFilterContext(
            string jobId,
            Dictionary<string, string> job, 
            Action enqueueAction)
        {
            JobId = jobId;
            Job = job;
            EnqueueAction = enqueueAction;
        }

        public string JobId { get; private set; }
        public Dictionary<string, string> Job { get; private set; }
        public Action EnqueueAction { get; private set; }
    }
}