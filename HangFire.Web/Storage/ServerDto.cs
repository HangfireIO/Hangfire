using System;

namespace HangFire.Web
{
    internal class ServerDto
    {
        public string Name { get; set; }
        public int Concurrency { get; set; }
        public string Queue { get; set; }
        public DateTime StartedAt { get; set; }
    }
}