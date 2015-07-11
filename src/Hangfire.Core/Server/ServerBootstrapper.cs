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
    internal class ServerBootstrapper : IBackgroundProcess
    {
        private const string BootstrapperId = "{4deecd4f-19f6-426b-aa87-6cd1a03eaa48}";
        private static readonly TimeSpan MutexWaitTimeout = TimeSpan.FromSeconds(10);

        private readonly IEnumerable<ILongRunningProcess> _processes;

        public ServerBootstrapper(IEnumerable<ILongRunningProcess> processes)
        {
            if (processes == null) throw new ArgumentNullException("processes");
            _processes = processes;
        }

        public void Execute(BackgroundProcessContext context)
        {
            using (var mutex = new MutexWrapper(context.ServerId))
            {
                // Do not allow to run multiple servers with the same ServerId on same
                // machine, fixes https://github.com/odinserj/Hangfire/issues/112.
                if (!mutex.WaitOne(MutexWaitTimeout, context.CancellationToken))
                {
                    throw new InvalidOperationException(String.Format(
                        "Global mutex for Server Id '{0}' could not be acquired. Please ensure there are no any other instances with the same Server Id.",
                        context.ServerId));
                }

                context.CancellationToken.ThrowIfCancellationRequested();

                using (var connection = context.Storage.GetConnection())
                {
                    var serverContext = new ServerContext();

                    if (context.ServerData.ContainsKey("Queues"))
                    {
                        var array = context.ServerData["Queues"] as string[];
                        if (array != null) { serverContext.Queues = array; }
                    }

                    if (context.ServerData.ContainsKey("WorkerCount"))
                    {
                        serverContext.WorkerCount = (int)context.ServerData["WorkerCount"];
                    }
                    
                    connection.AnnounceServer(context.ServerId, serverContext);
                }

                try
                {
                    var tasks = _processes
                        .Select(process => process.CreateTask(context))
                        .ToArray();

                    Task.WaitAll(tasks);
                }
                finally
                {
                    using (var connection = context.Storage.GetConnection())
                    {
                        connection.RemoveServer(context.ServerId);
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
