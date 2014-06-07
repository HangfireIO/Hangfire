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
    public class ServerBootstrapper : IServerComponent, IDisposable
    {
        private const string BootstrapperId = "{4deecd4f-19f6-426b-aa87-6cd1a03eaa48}";
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerBootstrapper));

        private readonly JobStorage _storage;
        private readonly string _serverId;
        private readonly ServerContext _context;
        private readonly Lazy<IServerSupervisor> _supervisorFactory;

        private readonly Mutex _globalMutex;

        public ServerBootstrapper(
            string serverId,
            ServerContext context,
            JobStorage storage,
            Lazy<IServerSupervisor> supervisorFactory)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (context == null) throw new ArgumentNullException("context");
            if (supervisorFactory == null) throw new ArgumentNullException("supervisorFactory");

            _storage = storage;
            _serverId = serverId;
            _context = context;
            _supervisorFactory = supervisorFactory;

            _globalMutex = new Mutex(false, String.Format(@"Global\{0}_{1}", BootstrapperId, _serverId));
        }

        public void Execute(CancellationToken cancellationToken)
        {
            // Do not allow to run multiple servers with the same ServerId on same
            // machine, fixes https://github.com/odinserj/HangFire/issues/112.
            WaitHandle.WaitAny(new[] { _globalMutex, cancellationToken.WaitHandle });
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                using (var connection = _storage.GetConnection())
                {
                    connection.AnnounceServer(_serverId, _context);
                }

                try
                {
                    using (_supervisorFactory.Value)
                    {
                        Logger.Info("Starting server components...");
                        _supervisorFactory.Value.Start();

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
            finally
            {
                _globalMutex.ReleaseMutex();
            }
        }

        public override string ToString()
        {
            return "Server Core";
        }

        public void Dispose()
        {
            _globalMutex.Dispose();
        }
    }
}
