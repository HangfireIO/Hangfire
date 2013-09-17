namespace HangFire.Server
{
    public class WorkerContext
    {
        public WorkerContext(ServerContext serverContext, int workerNumber)
        {
            ServerContext = serverContext;
            WorkerNumber = workerNumber;
        }

        public ServerContext ServerContext { get; private set; }
        public int WorkerNumber { get; private set; }
    }
}