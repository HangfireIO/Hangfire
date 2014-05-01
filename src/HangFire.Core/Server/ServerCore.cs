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
using Common.Logging;

namespace HangFire.Server
{
    public class ServerCore : IServerComponent
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerCore));

        private readonly JobStorage _storage;
        private readonly string _serverId;
        private readonly ServerContext _context;
        private readonly Lazy<IServerComponentRunner> _runner;

        public ServerCore(
            string serverId,
            ServerContext context,
            JobStorage storage,
            Lazy<IServerComponentRunner> runner)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (context == null) throw new ArgumentNullException("context");
            if (runner == null) throw new ArgumentNullException("runner");

            _storage = storage;
            _serverId = serverId;
            _context = context;
            _runner = runner;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            {
                connection.AnnounceServer(_serverId, _context);
            }
            
            try
            {
                using (_runner.Value)
                {
                    Logger.Info("Starting server components...");
                    _runner.Value.Start();

                    cancellationToken.WaitHandle.WaitOne();

                    Logger.Info("Stopping server components...");
                }
            }
            finally
            {
                using (var connection = _storage.GetConnection())
                {
                    connection.RemoveServer(_serverId);
                }
            }
        }

        public override string ToString()
        {
            return "Server Core";
        }
    }
}
