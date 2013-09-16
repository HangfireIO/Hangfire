using System;
using System.Collections.Generic;

namespace HangFire
{
    public class JobEnqueuedContext
    {
        public JobEnqueuedContext(string jobId, Dictionary<string, string> job, Exception exception)
        {
            JobId = jobId;
            Job = job;
            Exception = exception;
        }

        public string JobId { get; private set; }
        public Dictionary<string, string> Job { get; set; }
        public Exception Exception { get; private set; }
    }
}