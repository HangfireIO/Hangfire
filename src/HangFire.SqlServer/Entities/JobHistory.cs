using System;

namespace HangFire.SqlServer.Entities
{
    internal class JobHistory
    {
        public int JobId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Data { get; set; }
    }
}