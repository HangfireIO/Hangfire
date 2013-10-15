using System.Collections.Generic;

namespace HangFire.Server
{
    public class ServerContext
    {
        public ServerContext(string serverName, IList<string> queues, int workersCount)
        {
            ServerName = serverName;
            Queues = queues;
            WorkersCount = workersCount;
        }

        public string ServerName { get; private set; }
        public IList<string> Queues { get; private set; }
        public int WorkersCount { get; private set; }
    }
}
