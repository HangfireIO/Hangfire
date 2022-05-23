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
using Hangfire.Processing;

namespace Hangfire.Server
{
    public sealed class BackgroundProcessingServerOptions
    {
        internal static TimeSpan DefaultStopTimeout = TimeSpan.Zero;
        internal static TimeSpan DefaultLastChanceTimeout = TimeSpan.FromSeconds(1);
        internal static TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(30);

        private Func<int, TimeSpan> _retryDelay;

        public BackgroundProcessingServerOptions()
        {
            HeartbeatInterval = DefaultHeartbeatInterval;
            ServerCheckInterval = ServerWatchdog.DefaultCheckInterval;
            ServerTimeout = ServerWatchdog.DefaultServerTimeout;

            CancellationCheckInterval = ServerJobCancellationWatcher.DefaultCheckInterval;

            RetryDelay = BackgroundExecutionOptions.GetBackOffMultiplier;
            RestartDelay = TimeSpan.FromSeconds(15);

            StopTimeout = DefaultStopTimeout;
            ShutdownTimeout = BackgroundProcessingServer.DefaultShutdownTimeout;
            LastChanceTimeout = DefaultLastChanceTimeout;
        }

        public TimeSpan HeartbeatInterval { get; set; }
        public TimeSpan ServerCheckInterval { get; set; }
        public TimeSpan ServerTimeout { get; set; }
        public TimeSpan CancellationCheckInterval { get; set; }
        public string ServerName { get; set; }

        public Func<int, TimeSpan> RetryDelay
        {
            get => _retryDelay;
            set => _retryDelay = value ?? throw new ArgumentNullException(nameof(value));
        }

        public TimeSpan StopTimeout { get; set; }
        public TimeSpan ShutdownTimeout { get; set; }
        public TimeSpan LastChanceTimeout { get; set; }
        public TimeSpan RestartDelay { get; set; }
    }
}