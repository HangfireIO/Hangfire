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
using System.Diagnostics;
using System.Linq;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire
{
    public class BackgroundJobServer : IServerSupervisor
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly JobStorage _storage;
        private readonly JobActivator _jobActivator;
        private readonly BackgroundJobServerOptions _options;

        private readonly string _serverId;
        private readonly IServerSupervisor _bootstrapSupervisor;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class
        /// with default options and <see cref="JobStorage.Current"/> storage.
        /// </summary>
        public BackgroundJobServer()
            : this(new BackgroundJobServerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class
        /// with default options and the given storage.
        /// </summary>
        /// <param name="storage">The storage</param>
        public BackgroundJobServer(JobStorage storage)
            : this(new BackgroundJobServerOptions(), storage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class
        /// with the given options and <see cref="JobStorage.Current"/> storage.
        /// </summary>
        /// <param name="options">Server options</param>
        public BackgroundJobServer(BackgroundJobServerOptions options)
            : this(options, JobStorage.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class
        /// with the specified options, the given storage and <see cref="JobActivator.Current"/> job activator.
        /// </summary>
        /// <param name="options">Server options</param>
        /// <param name="storage">The storage</param>
        public BackgroundJobServer(BackgroundJobServerOptions options, JobStorage storage)
            : this(options, storage, JobActivator.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class
        /// with the specified options, the given storage and the given job activator.
        /// </summary>
        /// <param name="options">Server options</param>
        /// <param name="storage">The storage</param>
        /// <param name="jobActivator">The job activator</param>
        public BackgroundJobServer(BackgroundJobServerOptions options, JobStorage storage, JobActivator jobActivator)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (storage == null) throw new ArgumentNullException("storage");
            if (jobActivator == null) throw new ArgumentNullException("jobActivator");

            _options = options;
            _storage = storage;
            _jobActivator = jobActivator;

            _serverId = String.Format("{0}:{1}", _options.ServerName.ToLowerInvariant(), Process.GetCurrentProcess().Id);

            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            _bootstrapSupervisor = GetBootstrapSupervisor();

            Logger.Info("Starting Hangfire Server");
            Logger.InfoFormat("Using job storage: '{0}'.", _storage);

            _storage.WriteOptionsToLog(Logger);
            _options.WriteToLog(Logger);

            _bootstrapSupervisor.Start();
        }

        [Obsolete("This method is a stub. There is no need to call the `Start` method. Will be removed in version 2.0.0.")]
        public void Start()
        { 
        }

        [Obsolete("This method is a stub. Please call the `Dispose` method instead. Will be removed in version 2.0.0.")]
        public void Stop()
        {
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

            return CreateSupervisor(
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
            var performanceProcess = new DefaultJobPerformanceProcess(_jobActivator);
            var stateMachineFactory = new StateMachineFactory(_storage);

            for (var i = 0; i < _options.WorkerCount; i++)
            {
                var context = new WorkerContext(_serverId, _options.Queues, i + 1);
                yield return new Worker(context, _storage, performanceProcess, stateMachineFactory);
            }

            yield return new ServerHeartbeat(_storage, _serverId);
            yield return new SchedulePoller(_storage, stateMachineFactory, _options.SchedulePollingInterval);
            yield return new ServerWatchdog(_storage, _options.ServerWatchdogOptions);

            yield return new RecurringJobScheduler(
                _storage, 
                new BackgroundJobClient(_storage, stateMachineFactory),
                new ScheduleInstantFactory(),
                new EveryMinuteThrottler());
        }

        private static ServerSupervisor CreateSupervisor(IServerComponent component)
        {
            return CreateSupervisor(component, new ServerSupervisorOptions());
        }

        private static ServerSupervisor CreateSupervisor(IServerComponent component, ServerSupervisorOptions options)
        {
            return new ServerSupervisor(new AutomaticRetryServerComponentWrapper(component), options);
        }
    }
}
