using System;
using System.Collections.Generic;

namespace HangFire.Storage
{
    public class ScheduleDto
    {
        public DateTime ScheduledAt { get; set; }
        public string Type { get; set; }
        public string Queue { get; set; }
        public Dictionary<string, string> Args { get; set; }
    }
}