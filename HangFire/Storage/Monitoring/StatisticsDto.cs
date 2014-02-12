namespace HangFire.Storage.Monitoring
{
    public class StatisticsDto
    {
        public long Servers { get; set; }
        public long Enqueued { get; set; }
        public long Queues { get; set; }
        public long Scheduled { get; set; }
        public long Processing { get; set; }
        public long Succeeded { get; set; }
        public long Failed { get; set; }
    }
}
