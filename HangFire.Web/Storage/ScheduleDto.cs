using System;

namespace HangFire.Web
{
    internal class ScheduleDto
    {
        public DateTime ScheduledAt { get; set; }
        public string Type { get; set; }
        public string Queue { get; set; }
        public bool InScheduledState { get; set; }
    }
}