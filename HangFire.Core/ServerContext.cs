namespace HangFire
{
    public class ServerContext
    {
        public ServerContext(string serverName, string queueName, int workersCount)
        {
            ServerName = serverName;
            QueueName = queueName;
            WorkersCount = workersCount;
        }

        public string ServerName { get; private set; }
        public string QueueName { get; private set; }
        public int WorkersCount { get; private set; }
    }
}
