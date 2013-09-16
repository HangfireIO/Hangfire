using System.Collections.Generic;

namespace HangFire
{
    public class JobEnqueueingContext
    {
        public JobEnqueueingContext(string jobId, Dictionary<string, string> job)
        {
            JobId = jobId;
            Job = job;
        }

        public string JobId { get; private set; }
        public Dictionary<string, string> Job { get; private set; }

        public bool Canceled { get; private set; }

        public void Cancel()
        {
            Canceled = true;
        }
    }
}