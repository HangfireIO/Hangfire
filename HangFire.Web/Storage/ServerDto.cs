using System;
using System.Collections.Generic;

namespace HangFire.Web
{
    internal class ServerDto
    {
        public string Name { get; set; }
        public int WorkersCount { get; set; }
        public DateTime StartedAt { get; set; }
        public IList<string> Queues { get; set; }
        public DateTime? Heartbeat { get; set; }
    }
}