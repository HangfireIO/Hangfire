// This file is part of Hangfire. Copyright � 2013-2014 Sergey Odinokov.
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