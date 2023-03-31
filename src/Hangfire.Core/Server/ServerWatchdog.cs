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