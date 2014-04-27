using System;

namespace HangFire.Redis.Components
{
    internal class FetchedJobsWatcherOptions
    {
        public FetchedJobsWatcherOptions()
        {
            FetchedLockTimeout = TimeSpan.FromMinutes(1);
            CheckedTimeout = TimeSpan.FromMinutes(1);
            SleepTimeout = TimeSpan.FromMinutes(1);
            JobTimeout = TimeSpan.FromMinutes(15);
        }

        public TimeSpan FetchedLockTimeout { get; set; }
        public TimeSpan CheckedTimeout { get; set; }
        public TimeSpan SleepTimeout { get; set; }
        public TimeSpan JobTimeout { get; set; }
    }
}