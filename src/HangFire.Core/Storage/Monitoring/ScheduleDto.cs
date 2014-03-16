using System;
using HangFire.Common;

namespace HangFire.Storage.Monitoring
{
    public class ScheduleDto
    {
        public ScheduleDto()
        {
            InScheduledState = true;
        }

        public MethodData MethodData { get; set; }
        public DateTime ScheduledAt { get; set; }
        public bool InScheduledState { get; set; }
    }
}