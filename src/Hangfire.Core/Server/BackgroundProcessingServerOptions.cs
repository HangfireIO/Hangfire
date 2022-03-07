// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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