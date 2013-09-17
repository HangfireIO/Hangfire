using System;
using System.Collections.Generic;

namespace HangFire.Storage
{
    public class SucceededJobDto
    {
        public string Type { get; set; }
        public string Queue { get; set; }
        public Dictionary<string, string> Args { get; set; }
        public DateTime? SucceededAt { get; set; }
        public TimeSpan Latency { get; set; }
    }
}