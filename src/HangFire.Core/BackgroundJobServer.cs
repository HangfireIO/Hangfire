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
using Common.Logging;
using HangFire.Server;
using HangFire.States;

namespace HangFire
{
    public class BackgroundJobServer : IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BackgroundJobServer));

        private readonly JobStorage _storage;
        private readonly BackgroundJobServerOptions _options;

        private readonly string _serverId;
        private readonly IServerComponentRunner _serverRunner;

        public BackgroundJobServer()
            : this(new BackgroundJobServerOptions())
        {
        }

        public BackgroundJobServer(BackgroundJobServerOptions options)
            : this(options, JobStorage.Current)
        {
        }

        public BackgroundJobServer(BackgroundJobServerOptions options, JobStorage storage)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (storage == null) throw new ArgumentNullException("storage");

            _options = options;
            _storage = storage;

            _serverId = String.Format("{0}:{1}", _options.ServerName.ToLowerInvariant(), Process.GetCurrentProcess().Id);

            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            _serverRunner = GetServerRunner();
        }

        public void Start()
        {
            Logger.Info("Starting HangFire Server...");
            Logger.InfoFormat("Using job storage: '{0}'.", _storage);

            _options.Log(Logger);

            _serverRunner.Start();
        }

        public void Stop()
        {
            _serverRunner.Stop();
        }

        public virtual void Dispose()
        {
            _serverRunner.Dispose();
            Logger.Info("HangFire Server stopped.");
        }

        internal virtual IServerComponentRunner GetServerRunner()
        {
            var context = new ServerContext
            {
                Queues = _options.Queues,
                WorkerCount = _options.WorkerCount
            };

            var server = new ServerCore(
                _serverId, 
                context, 
                _storage, 
                new Lazy<IServerComponentRunner>(GetServerComponentsRunner));

            return new ServerComponentRunner(
                server, 
                new ServerComponentRunnerOptions
                {
                    ShutdownTimeout = _options.ShutdownTimeout
                });
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
            var stateMachineFactory = new StateMachineFactory(_storage);
            var sharedWorkerContext = new SharedWorkerContext(
                _serverId,
                _options.Queues,
                _storage,
                new JobPerformanceProcess(),
                JobActivator.Current,
                stateMachineFactory);

            yield return new ServerComponentRunner(new WorkerManager(sharedWorkerContext, _options.WorkerCount));
            yield return new ServerComponentRunner(new ServerHeartbeat(_storage, _serverId));
            yield return new ServerComponentRunner(new ServerWatchdog(_storage));

            yield return new ServerComponentRunner(
                new SchedulePoller(_storage, stateMachineFactory, _options.SchedulePollingInterval));
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
