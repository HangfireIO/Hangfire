using System;

namespace HangFire.SqlServer.Entities
{
    public class JobHistory
    {
        public Guid JobId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Data { get; set; }
    }
}