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
using System.Collections.Generic;
using Hangfire.Logging;

namespace Hangfire.Server
{
    internal class ServerHeartbeat : IBackgroundProcess
    {
        public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(30);

        private static readonly ILog Logger = LogProvider.For<ServerHeartbeat>();

        private readonly TimeSpan _heartbeatInterval;
        private readonly ServerContext _serverContext;

        public ServerHeartbeat(TimeSpan heartbeatInterval, ServerContext serverContext)
        {
            _heartbeatInterval = heartbeatInterval;
            _serverContext = serverContext;
        }

        public void Execute(BackgroundProcessContext context)
        {
            using (var connection = context.Storage.GetConnection())
            {
                if (!connection.ServerPresent(context.ServerId))
                {
                    connection.AnnounceServer(context.ServerId, _serverContext);
                }
                connection.Heartbeat(context.ServerId);
            }

            context.Wait(_heartbeatInterval);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}