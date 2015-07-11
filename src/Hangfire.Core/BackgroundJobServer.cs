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
using Hangfire.Client;
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
            var bootstrapper = new ServerBootstrapper(GetProcesses());

            var context = new BackgroundProcessContext(_serverId, _storage, _cts.Token);
            context.ServerData["Queues"] = _options.Queues;
            context.ServerData["WorkerCount"] = _options.WorkerCount;

            return WrapProcess(bootstrapper).CreateTask(context);
        }

        internal IEnumerable<ILongRunningProcess> GetProcesses()
        {
            var processes = new List<ILongRunningProcess>();

            processes.AddRange(GetCommonProcesses().Select(WrapProcess));
            processes.AddRange(_storage.GetComponents().Select(WrapProcess));

            return processes;
        }

        private IEnumerable<IBackgroundProcess> GetCommonProcesses()
        {
            for (var i = 0; i < _options.WorkerCount; i++)
            {
                yield return new Worker(new WorkerContext(_options.Queues, i + 1));
            }

            yield return new ServerHeartbeat();
            yield return new SchedulePoller(_options.SchedulePollingInterval);
            yield return new ServerWatchdog(_options.ServerWatchdogOptions);

            yield return new RecurringJobScheduler();
        }

        private static ILongRunningProcess WrapProcess(ILongRunningProcess process)
        {
            return new InfiniteLoopProcess(new AutomaticRetryProcess(process));
        }
    }
}
