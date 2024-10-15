// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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

// ReSharper disable once CheckNamespace
namespace Hangfire.Server
{
    /// <exclude />
    [Obsolete("Please use `BackgroundJobServerOptions` properties instead. Will be removed in 2.0.0.")]
    public class ServerWatchdogOptions
    {
        private TimeSpan _serverTimeout;
        private TimeSpan _checkInterval;

        public ServerWatchdogOptions()
        {
            ServerTimeout = ServerWatchdog.DefaultServerTimeout;
            CheckInterval = ServerWatchdog.DefaultCheckInterval;
        }

        public TimeSpan ServerTimeout
        {
            get { return _serverTimeout; }
            set
            {
                if (value < TimeSpan.Zero || value > ServerWatchdog.MaxServerTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"ServerTimeout must be either non-negative and equal to or less than {ServerWatchdog.MaxServerTimeout.Hours} hours");
                }

                _serverTimeout = value;
            }
        }

        public TimeSpan CheckInterval
        {
            get { return _checkInterval; }
            set
            {
                if (value < TimeSpan.Zero || value > ServerWatchdog.MaxServerCheckInterval)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"CheckInterval must be either non-negative and equal to or less than {ServerWatchdog.MaxServerCheckInterval.Hours} hours");

                };
                _checkInterval = value;
            }
        }
    }
}