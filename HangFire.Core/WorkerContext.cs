using System.Collections.Generic;

namespace HangFire
{
    public class WorkerContext
    {
        public WorkerContext(
            string serverName, 
            string workerName,
            string jobId,
            string jobType,
            IDictionary<string, string> jobProperties)
        {
            ServerName = serverName;
            WorkerName = workerName;
            JobId = jobId;
            JobType = jobType;
            JobProperties = jobProperties;
        }

        public string ServerName { get; private set; }
        public string WorkerName { get; private set; }

        public string JobId { get; private set; }
        public string JobType { get; private set; }
        public IDictionary<string, string> JobProperties { get; private set; }
    }
}