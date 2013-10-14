namespace HangFire.Server
{
    public class ServerContext
    {
        public ServerContext(string serverName, string queue, int workersCount)
        {
            ServerName = serverName;
            Queue = queue;
            WorkersCount = workersCount;
        }

        public string ServerName { get; private set; }
        public string Queue { get; private set; }
        public int WorkersCount { get; private set; }
    }
}
