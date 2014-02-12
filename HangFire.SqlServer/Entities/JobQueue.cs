using System;

namespace HangFire.SqlServer.Entities
{
    public class JobQueue
    {
        public Guid JobId { get; set; }
        public string QueueName { get; set; }
        public DateTime? FetchedAt { get; set; }
        public DateTime? CheckedAt { get; set; }
    }
}