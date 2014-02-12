namespace HangFire.SqlServer.Entities
{
    public class ServerData
    {
        public int WorkerCount { get; set; }
        public string[] Queues { get; set; }
    }
}
