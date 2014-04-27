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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Common.Logging;

namespace HangFire.Server
{
    public class JobServer : IDisposable
    {
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);
        private const int RetryAttempts = 10;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(JobServer));

        private readonly int _workerCount;
        private readonly string _serverName;
        private readonly string[] _queues;

        private readonly Thread _serverThread;

        private WorkerManager _manager;
        private readonly IList<ThreadWrapper> _components = new List<ThreadWrapper>(); 

        private readonly JobStorage _storage;

        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        public JobServer(
            JobStorage storage,
            string serverName,
            int workerCount,
            string[] queues)
        {
            _storage = storage;
            _workerCount = workerCount;
            _queues = queues;
            _serverName = serverName;

            if (queues == null) throw new ArgumentNullException("queues");

            _serverThread = new Thread(RunServer)
                {
                    Name = typeof(JobServer).Name,
                    IsBackground = true
                };
            _serverThread.Start();
        }

        public void Dispose()
        {
            _stopped.Set();
            _serverThread.Join();
        }

        private void StartServer()
        {
            var storage = JobStorage.Current;

            _manager = new WorkerManager(_storage, _serverName, _queues, _workerCount);

            var components = storage.GetComponents();
            foreach (var component in components)
            {
                _components.Add(new ThreadWrapper(component));
            }
        }

        private void StopServer()
        {
            foreach (var component in _components.Reverse())
            {
                component.Dispose();
            }

            _manager.Dispose();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        private void RunServer()
        {
            try
            {
                Logger.Info("Starting HangFire Server...");

                using (var connection = _storage.GetConnection())
                {
                    var context = new ServerContext
                    {
                        WorkerCount = _workerCount,
                        Queues = _queues,
                    };

                    connection.AnnounceServer(_serverName, context);
                }
                StartServer();

                Logger.Info("HangFire Server has been started.");

                while (true)
                {
                    RetryOnException(
                        () =>
                        {
                            using (var connection = _storage.GetConnection())
                            {
                                connection.Heartbeat(_serverName);
                            }
                        }, 
                        _stopped);

                    if (_stopped.WaitOne(HeartbeatInterval))
                    {
                        break;
                    }
                }

                Logger.Info("Stopping HangFire Server...");

                StopServer();

                using (var connection = _storage.GetConnection())
                {
                    connection.RemoveServer(_serverName);
                }

                Logger.Info("HangFire Server has been stopped.");
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected exception caught.", ex);
            }
        }

        public static void RetryOnException(Action action, WaitHandle waitHandle)
        {
            for (var i = 0; i < RetryAttempts; i++)
            {
                try
                {
                    action();

                    // Break the loop after successful invocation.
                    break;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Break the loop after the retry attempts number exceeded.
                    if (i == RetryAttempts - 1) throw;

                    // Break the loop when the wait handle was signaled.
                    if (SleepBackOffMultiplier(i, waitHandle))
                    {
                        break;
                    }
                }
            }
        }

        private static bool SleepBackOffMultiplier(int i, WaitHandle waitHandle)
        {
            //exponential/random retry back-off.
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var nextTry = rand.Next(
                (int)Math.Pow(i, 2), (int)Math.Pow(i + 1, 2) + 1);

            return waitHandle.WaitOne(TimeSpan.FromSeconds(nextTry));
        }
    }
}