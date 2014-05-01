// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;

namespace HangFire.Server
{
    public class ServerHeartbeat : IServerComponent
    {
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

        private readonly JobStorage _storage;
        private readonly string _serverId;

        public ServerHeartbeat(JobStorage storage, string serverId)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (serverId == null) throw new ArgumentNullException("serverId");

            _storage = storage;
            _serverId = serverId;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            {
                connection.Heartbeat(_serverId);
            }

            cancellationToken.WaitHandle.WaitOne(HeartbeatInterval);
        }

        public override string ToString()
        {
            return "Server Heartbeat";
        }
    }
}