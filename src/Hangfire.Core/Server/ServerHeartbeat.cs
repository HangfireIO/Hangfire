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

namespace Hangfire.Server
{
    internal class ServerHeartbeat : IBackgroundProcess
    {
        public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(30);

        private readonly TimeSpan _heartbeatInterval;

        public ServerHeartbeat(TimeSpan heartbeatInterval)
        {
            _heartbeatInterval = heartbeatInterval;
        }

        public void Execute(BackgroundProcessContext context)
        {
            using (var connection = context.Storage.GetConnection())
            {
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