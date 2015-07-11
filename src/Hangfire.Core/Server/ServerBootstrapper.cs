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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Server
{
    public class ServerBootstrapper : IServerComponent
    {
        private const string BootstrapperId = "{4deecd4f-19f6-426b-aa87-6cd1a03eaa48}";
        private static readonly TimeSpan MutexWaitTimeout = TimeSpan.FromSeconds(10);

        private readonly JobStorage _storage;
        private readonly string _serverId;
        private readonly ServerContext _context;
        private readonly IEnumerable<IServerComponent> _components;

        public ServerBootstrapper(
            string serverId,
            ServerContext context,
            JobStorage storage,
            IEnumerable<IServerComponent> components)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (context == null) throw new ArgumentNullException("context");
            if (components == null) throw new ArgumentNullException("components");

            _storage = storage;
            _serverId = serverId;
            _context = context;
            _components = components;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var mutex = new MutexWrapper(_serverId))
            {
                // Do not allow to run multiple servers with the same ServerId on same
                // machine, fixes https://github.com/odinserj/Hangfire/issues/112.
                if (!mutex.WaitOne(MutexWaitTimeout, cancellationToken))
                {
                    throw new InvalidOperationException(String.Format(
                        "Global mutex for Server Id '{0}' could not be acquired. Please ensure there are no any other instances with the same Server Id.",
                        _serverId));
                }

                cancellationToken.ThrowIfCancellationRequested();

                using (var connection = _storage.GetConnection())
                {
                    connection.AnnounceServer(_serverId, _context);
                }

                try
                {
                    var tasks = _components
                        .Select(component => component.CreateTask(cancellationToken))
                        .ToArray();

                    Task.WaitAll(tasks);
                }
                finally
                {
                    using (var connection = _storage.GetConnection())
                    {
                        connection.RemoveServer(_serverId);
                    }
                }
            }
        }

        public override string ToString()
        {
            return "Server Bootstrapper";
        }

        private class MutexWrapper : IDisposable
        {
            private readonly Mutex _mutex;
            private bool _acquired;

            public MutexWrapper(string serverId)
            {
                if (!RunningWithMono())
                {
                    _mutex = new Mutex(false, String.Format(@"Global\{0}_{1}", BootstrapperId, serverId));
                }
            }

            public bool WaitOne(TimeSpan timeout, CancellationToken cancellationToken)
            {
                if (_mutex == null) return true;

                var waitResult = WaitHandle.WaitAny(new[] { _mutex, cancellationToken.WaitHandle }, timeout);
                _acquired = waitResult == 0;

                return waitResult != WaitHandle.WaitTimeout;
            }

            public void Dispose()
            {
                if (_mutex == null) return;

                if (_acquired)
                {
                    _mutex.ReleaseMutex();
                }

                _mutex.Dispose();
            }

            private static bool RunningWithMono()
            {
                return Type.GetType("Mono.Runtime") != null;
            }
        }
    }
}
