using System;

namespace HangFire.SqlServer.Entities
{
    public class JobHistory
    {
        public int JobId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Data { get; set; }
    }
}