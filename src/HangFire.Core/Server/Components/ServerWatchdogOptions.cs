using System;

namespace HangFire.Server.Components
{
    public class ServerWatchdogOptions
    {
        public ServerWatchdogOptions()
        {
            ServerTimeout = TimeSpan.FromMinutes(1);
            CheckInterval = TimeSpan.FromMinutes(5);
        }

        public TimeSpan ServerTimeout { get; set; }
        public TimeSpan CheckInterval { get; set; }
    }
}