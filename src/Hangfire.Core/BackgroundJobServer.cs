﻿// This file is part of Hangfire.
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
using System.Diagnostics;
using System.Linq;
using Common.Logging;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire
{
    public class BackgroundJobServer : IServerSupervisor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BackgroundJobServer));

        private readonly JobStorage _storage;
        private readonly BackgroundJobServerOptions _options;

        private readonly string _serverId;
        private readonly IServerSupervisor _bootstrapSupervisor;

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
            _bootstrapSupervisor = GetBootstrapSupervisor();
        }

        public void Start()
        {
            Logger.Info("Starting Hangfire Server...");
            Logger.InfoFormat("Using job storage: '{0}'.", _storage);
            
            _storage.WriteOptionsToLog(Logger);
            _options.WriteToLog(Logger);

            _bootstrapSupervisor.Start();
        }

        public void Stop()
        {
            _bootstrapSupervisor.Stop();
        }

        public virtual void Dispose()
        {
            _bootstrapSupervisor.Dispose();
            Logger.Info("Hangfire Server stopped.");
        }

        internal virtual IServerSupervisor GetBootstrapSupervisor()
        {
            var context = new ServerContext
            {
                Queues = _options.Queues,
                WorkerCount = _options.WorkerCount
            };

            var bootstrapper = new ServerBootstrapper(
                _serverId, 
                context, 
                _storage, 
                new Lazy<IServerSupervisor>(GetSupervisors));

            return new ServerSupervisor(
                bootstrapper, 
                new ServerSupervisorOptions
                {
                    ShutdownTimeout = _options.ShutdownTimeout
                });
        }

        internal ServerSupervisorCollection GetSupervisors()
        {
            var supervisors = new List<IServerSupervisor>();

            supervisors.AddRange(GetCommonComponents().Select(CreateSupervisor));
            supervisors.AddRange(_storage.GetComponents().Select(CreateSupervisor));

            return new ServerSupervisorCollection(supervisors);
        }

        private IEnumerable<IServerComponent> GetCommonComponents()
        {
            var stateMachineFactory = new StateMachineFactory(_storage);
            var sharedWorkerContext = new SharedWorkerContext(
                _serverId,
                _options.Queues,
                _storage,
                new JobPerformanceProcess(),
                JobActivator.Current,
                stateMachineFactory);

            yield return new WorkerManager(sharedWorkerContext, _options.WorkerCount);
            yield return new ServerHeartbeat(_storage, _serverId);
            yield return new ServerWatchdog(_storage);
            yield return new SchedulePoller(_storage, stateMachineFactory, _options.SchedulePollingInterval);

            yield return new RecurringJobScheduler(
                _storage, 
                new BackgroundJobClient(_storage, stateMachineFactory),
                new ScheduleInstantFactory(),
                new EveryMinuteThrottler());
        }

        private static ServerSupervisor CreateSupervisor(IServerComponent component)
        {
            return new ServerSupervisor(new AutomaticRetryServerComponentWrapper(component));
        }
    }
}
