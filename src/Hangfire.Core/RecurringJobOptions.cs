using System;
using Hangfire.States;

namespace Hangfire
{
    public class RecurringJobOptions
    {
        public RecurringJobOptions()
        {
            TimeZone = TimeZoneInfo.Utc;
            QueueName = EnqueuedState.DefaultQueue;
        }

        public TimeZoneInfo TimeZone { get; set; }
        public string QueueName { get; set; }
    }
}
