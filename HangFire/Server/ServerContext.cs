using System.Collections.Generic;

namespace HangFire.Server
{
    public class ServerContext
    {
        public ServerContext(ServerContext context)
            : this(context.ServerName, context.Queues, context.WorkersCount)
        {
        }

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
