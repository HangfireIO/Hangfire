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
using System.Threading;

namespace Hangfire.Server
{
    public sealed class BackgroundProcessingServerOptions
    {
        private TimeSpan _shutdownTimeout;
        private TimeSpan _heartbeatInterval;
        private TimeSpan _serverCheckInterval;
        private TimeSpan _serverTimeout;

        public BackgroundProcessingServerOptions()
        {
            ShutdownTimeout = BackgroundProcessingServer.DefaultShutdownTimeout;
            HeartbeatInterval = ServerHeartbeat.DefaultHeartbeatInterval;
            ServerCheckInterval = ServerWatchdog.DefaultCheckInterval;
            ServerTimeout = ServerWatchdog.DefaultServerTimeout;
        }

        public TimeSpan ShutdownTimeout
        {
            get { return _shutdownTimeout; }
            set
            {
                if ((value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan) || value.TotalMilliseconds > Int32.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"ShutdownTimeout must be either equal to or less than {Int32.MaxValue} milliseconds and non-negative or infinite.");
                }
                _shutdownTimeout = value;
            }
        }

        public TimeSpan HeartbeatInterval
        {
            get { return _heartbeatInterval; }
            set
            {
                if (value < TimeSpan.Zero || value > ServerWatchdog.MaxServerCheckInterval)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"HeartbeatInterval must be either non-negative and equal to or less than {ServerWatchdog.MaxHeartbeatInterval.Hours} hours.");
                }
                _heartbeatInterval = value;
            }
        }

        public TimeSpan ServerCheckInterval
        {
            get { return _serverCheckInterval; }
            set
            {
                if (value < TimeSpan.Zero || value > ServerWatchdog.MaxServerCheckInterval)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"ServerCheckInterval must be either non-negative and equal to or less than {ServerWatchdog.MaxServerCheckInterval.Hours} hours.");
                }
                _serverCheckInterval = value;
            }
        }

        public TimeSpan ServerTimeout
        {
            get { return _serverTimeout; }
            set
            {
                if (value < TimeSpan.Zero || value > ServerWatchdog.MaxServerTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"ServerTimeout must be either non-negative and equal to or less than {ServerWatchdog.MaxServerTimeout.Hours} hours.");
                }
                _serverTimeout = value;
            }
        }

        public string ServerName { get; set; }
    }
}