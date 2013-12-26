using System;
using HangFire.Common;

namespace HangFire.Web
{
    internal class ScheduleDto
    {
        public JobMethod Method { get; set; }
        public DateTime ScheduledAt { get; set; }
        public bool InScheduledState { get; set; }
    }
}