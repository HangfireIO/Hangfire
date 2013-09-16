using System.Collections.Generic;

namespace HangFire
{
    public class WorkerContext
    {
        public WorkerContext(
            string serverName, 
            string workerName,
            string queueName)
        {
            ServerName = serverName;
            WorkerName = workerName;
            QueueName = queueName;
        }

        public string ServerName { get; private set; }
        public string WorkerName { get; private set; }
        public string QueueName { get; private set; }
    }
}