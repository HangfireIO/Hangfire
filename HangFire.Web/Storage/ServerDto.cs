using System;
using System.Collections.Generic;

namespace HangFire.Web
{
    internal class ServerDto
    {
        public string Name { get; set; }
        public HashSet<string> Queues { get; set; }
        public long DequeuedJobs { get; set; }
        public int TotalWorkers { get; set; }
        public IList<ServerInstanceDto> Instances { get; set; }
    }

    internal class ServerInstanceDto
    {
        public string Id { get; set; }
        public int WorkersCount { get; set; }
        public DateTime StartedAt { get; set; }
        public HashSet<string> Queues { get; set; }
    }
}