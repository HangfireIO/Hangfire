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
using System.Diagnostics;
using System.Linq;
using HangFire.Server;
using HangFire.States;

namespace HangFire
{
    public class BackgroundJobServer : IDisposable
    {
        private static readonly TimeSpan ServerShutdownTimeout = TimeSpan.FromSeconds(15);
        private static readonly int DefaultWorkerCount = Environment.ProcessorCount * 5;

        private readonly JobStorage _storage;
        private readonly string _serverId;
        private readonly int _workerCount;
        private readonly string[] _queues;
        private readonly IServerComponentRunner _serverRunner;

        public BackgroundJobServer(params string[] queues)
            : this(DefaultWorkerCount, queues)
        {
        }

        public BackgroundJobServer(int workerCount, params string[] queues)
            : this(workerCount, queues, JobStorage.Current)
        {
        }

        public BackgroundJobServer(int workerCount, string[] queues, JobStorage storage)
        {
            if (workerCount <= 0) throw new ArgumentOutOfRangeException("workerCount", "Worker count value must be more than zero.");
            if (queues == null) throw new ArgumentNullException("queues");
            if (storage == null) throw new ArgumentNullException("storage");

            _storage = storage;
            _workerCount = workerCount;
            _queues = queues.Length != 0 ? queues : new[] { EnqueuedState.DefaultQueue };

            _serverId = String.Format("{0}:{1}", Environment.MachineName.ToLowerInvariant(), Process.GetCurrentProcess().Id);

// ReSharper disable once DoNotCallOverridableMethodsInConstructor
            _serverRunner = GetServerRunner();
        }

        public virtual void Start()
        {
            _serverRunner.Start();
        }

        public virtual void Stop()
        {
            _serverRunner.Stop();
        }

        public void Dispose()
        {
            _serverRunner.Dispose();
        }

        internal virtual IServerComponentRunner GetServerRunner()
        {
            var context = new ServerContext
            {
                Queues = _queues,
                WorkerCount = _workerCount
            };

            var server = new JobServer(
                _serverId, 
                context, 
                _storage, 
                new Lazy<IServerComponentRunner>(GetServerComponentsRunner));

            return new ServerComponentRunner(
                server, 
                new ServerComponentRunnerOptions { ShutdownTimeout = ServerShutdownTimeout });
        }

        internal ServerComponentRunnerCollection GetServerComponentsRunner()
        {
            var componentRunners = new List<IServerComponentRunner>();

            componentRunners.AddRange(GetCommonComponentRunners());
            componentRunners.AddRange(GetStorageComponentRunners());

            return new ServerComponentRunnerCollection(componentRunners);
        }

        private IEnumerable<IServerComponentRunner> GetCommonComponentRunners()
        {
            yield return new ServerComponentRunner(
                new ServerHeartbeat(_storage, _serverId));

            yield return new WorkerManager(
                _serverId, _workerCount, _queues, _storage, new JobPerformanceProcess(), new StateMachineFactory(_storage));

            yield return new ServerComponentRunner(
                new ServerWatchdog(_storage));
        }

        private IEnumerable<IServerComponentRunner> GetStorageComponentRunners()
        {
            var components = _storage.GetComponents();

            return components
                .Select(component => new ServerComponentRunner(component))
                .ToArray();
        }
    }
}
