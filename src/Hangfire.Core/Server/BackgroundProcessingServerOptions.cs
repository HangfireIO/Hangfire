// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using Hangfire.Processing;

namespace Hangfire.Server
{
    public sealed class BackgroundProcessingServerOptions
    {
        internal static TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(15);
        internal static TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(30);

        private Func<int, TimeSpan> _retryDelay;

        public BackgroundProcessingServerOptions()
        {
            HeartbeatInterval = DefaultHeartbeatInterval;
            ServerCheckInterval = ServerWatchdog.DefaultCheckInterval;
            ServerTimeout = ServerWatchdog.DefaultServerTimeout; // todo watchdog should work only upon successful heartbeat
            RestartTimeout = TimeSpan.FromSeconds(15);
            RetryDelay = BackgroundExecutionOptions.GetBackOffMultiplier;
            ShutdownTimeout = DefaultShutdownTimeout;
            ForcedStopTimeout = TimeSpan.FromSeconds(1);
            AbortTimeout = TimeSpan.FromMilliseconds(100);
            ServerRetryInterval = TimeSpan.FromSeconds(15);
        }

        public TimeSpan HeartbeatInterval { get; set; }
        public TimeSpan ServerCheckInterval { get; set; }
        public TimeSpan ServerTimeout { get; set; }
        public TimeSpan RestartTimeout { get; set; }
        public string ServerName { get; set; }

        public Func<int, TimeSpan> RetryDelay
        {
            get { return _retryDelay; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _retryDelay = value;
            }
        }

        public TimeSpan ShutdownTimeout { get; set; }
        public TimeSpan ForcedStopTimeout { get; set; }
        public TimeSpan AbortTimeout { get; set; }
        public TimeSpan ServerRetryInterval { get; set; }
    }
}