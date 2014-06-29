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
using Common.Logging;

namespace Hangfire.Server
{
    public class ServerWatchdog : IServerComponent
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerWatchdog));

        private readonly JobStorage _storage;
        private readonly ServerWatchdogOptions _options;

        public ServerWatchdog(JobStorage storage)
            : this(storage, new ServerWatchdogOptions())
        {
        }

        public ServerWatchdog(JobStorage storage, ServerWatchdogOptions options)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (options == null) throw new ArgumentNullException("options");

            _storage = storage;
            _options = options;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            {
                var serversRemoved = connection.RemoveTimedOutServers(_options.ServerTimeout);
                if (serversRemoved != 0)
                {
                    Logger.Info(String.Format(
                        "{0} servers were removed due to timeout", 
                        serversRemoved));
                }
            }

            cancellationToken.WaitHandle.WaitOne(_options.CheckInterval);
        }

        public override string ToString()
        {
            return "Server Watchdog";
        }
    }
}