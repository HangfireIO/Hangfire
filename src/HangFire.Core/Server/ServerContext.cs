namespace HangFire.Server
{
    public class ServerContext
    {
        public int WorkerCount { get; set; }
        public string[] Queues { get; set; }
    }
}