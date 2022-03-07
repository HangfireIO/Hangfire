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
using Hangfire.Logging;

namespace Hangfire.Server
{
    internal class ServerWatchdog : IBackgroundProcess
    {
        public static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan DefaultServerTimeout = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan MaxServerTimeout = TimeSpan.FromHours(24);
        public static readonly TimeSpan MaxServerCheckInterval = TimeSpan.FromHours(24);
        public static readonly TimeSpan MaxHeartbeatInterval = TimeSpan.FromHours(24);

        private readonly ILog _logger = LogProvider.For<ServerWatchdog>();

        private readonly TimeSpan _checkInterval;
        private readonly TimeSpan _serverTimeout;

        public ServerWatchdog(TimeSpan checkInterval, TimeSpan serverTimeout)
        {
            _checkInterval = checkInterval;
            _serverTimeout = serverTimeout;
        }

        public void Execute(BackgroundProcessContext context)
        {
            using (var connection = context.Storage.GetConnection())
            {
                var serversRemoved = connection.RemoveTimedOutServers(_serverTimeout);
                if (serversRemoved != 0)
                {
                    _logger.Info($"{serversRemoved} servers were removed due to timeout");
                }
            }

            context.Wait(_checkInterval);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}