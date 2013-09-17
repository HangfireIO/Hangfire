using System.Collections.Generic;

namespace HangFire.Server
{
    public class WorkerContext
    {
        public WorkerContext(ServerContext serverContext, int workerNumber)
        {
            ServerContext = serverContext;
            WorkerNumber = workerNumber;

            Items = new Dictionary<string, object>();
        }

        public ServerContext ServerContext { get; private set; }
        public int WorkerNumber { get; private set; }
        public IDictionary<string, object> Items { get; private set; }
    }
}