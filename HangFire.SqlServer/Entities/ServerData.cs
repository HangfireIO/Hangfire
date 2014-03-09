namespace HangFire.SqlServer.Entities
{
    internal class ServerData
    {
        public int WorkerCount { get; set; }
        public string[] Queues { get; set; }
    }
}
