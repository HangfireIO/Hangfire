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
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire
{
    public class BackgroundJobServer : IDisposable
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly JobStorage _storage;
        private readonly BackgroundJobServerOptions _options;

        private readonly string _serverId;
        private readonly Task _bootstrapTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

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
        /// with the specified options and the given storage.
        /// </summary>
        /// <param name="options">Server options</param>
        /// <param name="storage">The storage</param>
        public BackgroundJobServer(BackgroundJobServerOptions options, JobStorage storage)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (storage == null) throw new ArgumentNullException("storage");

            _options = options;
            _storage = storage;

            _serverId = String.Format("{0}:{1}", _options.ServerName.ToLowerInvariant(), Process.GetCurrentProcess().Id);

            Logger.Info("Starting Hangfire Server");
            Logger.InfoFormat("Using job storage: '{0}'.", _storage);
            
            _storage.WriteOptionsToLog(Logger);
            _options.WriteToLog(Logger);

            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            _bootstrapTask = GetBootstrapTask();
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
            _cts.Cancel();

            if (!_bootstrapTask.Wait(_options.ShutdownTimeout))
            {
                Logger.WarnFormat("");
            }
            
            Logger.Info("Hangfire Server stopped.");
        }

        internal virtual Task GetBootstrapTask()
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
                GetComponents());

            return WrapComponent(bootstrapper).CreateTask(_cts.Token);
        }

        internal IEnumerable<IServerComponent> GetComponents()
        {
            var components = new List<IServerComponent>();

            components.AddRange(GetCommonComponents().Select(WrapComponent));
            components.AddRange(_storage.GetComponents().Select(WrapComponent));

            return components;
        }

        private IEnumerable<IServerComponent> GetCommonComponents()
        {
            var performanceProcess = new DefaultJobPerformanceProcess(JobActivator.Current);
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

        private static IServerComponent WrapComponent(IServerComponent component)
        {
            return new InfiniteLoopComponent(new AutomaticRetryServerComponentWrapper(component));
        }
    }
}
