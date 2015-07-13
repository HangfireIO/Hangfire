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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire
{
    public class BackgroundJobServer : IDisposable
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        
        private readonly Task _bootstrapTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // This field is only for compatibility reasons – we can't remove old ctors.
        // Should be removed in 2.0.0.
        private readonly BackgroundJobServer _innerServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class
        /// with default options and <see cref="JobStorage.Current"/> storage.
        /// </summary>
        [Obsolete("Please use the `BackgroundJobServer.StartNew` method instead. Will be removed in version 2.0.0.")]
        public BackgroundJobServer()
            : this(new BackgroundJobServerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class
        /// with default options and the given storage.
        /// </summary>
        /// <param name="storage">The storage</param>
        [Obsolete("Please use the `BackgroundJobServer.StartNew` method instead. Will be removed in version 2.0.0.")]
        public BackgroundJobServer([NotNull] JobStorage storage)
            : this(new BackgroundJobServerOptions(), storage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class
        /// with the given options and <see cref="JobStorage.Current"/> storage.
        /// </summary>
        /// <param name="options">Server options</param>
        [Obsolete("Please use the `BackgroundJobServer.StartNew` method instead. Will be removed in version 2.0.0.")]
        public BackgroundJobServer([NotNull] BackgroundJobServerOptions options)
            : this(options, JobStorage.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class
        /// with the specified options and the given storage.
        /// </summary>
        /// <param name="options">Server options</param>
        /// <param name="storage">The storage</param>
        [Obsolete("Please use the `BackgroundJobServer.StartNew` method instead. Will be removed in version 2.0.0.")]
        public BackgroundJobServer([NotNull] BackgroundJobServerOptions options, [NotNull] JobStorage storage)
        {
            _innerServer = StartNew(storage, options);
        }

        public BackgroundJobServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes)
            : this(storage, processes, new Dictionary<string, object>())
        {
        }

        public BackgroundJobServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> serverData)
            : this(storage, processes.Cast<ILongRunningProcess>(), serverData)
        {
        }

        internal BackgroundJobServer(
            [NotNull] JobStorage storage, 
            [NotNull] IEnumerable<ILongRunningProcess> processes,
            [NotNull] IDictionary<string, object> serverData)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (processes == null) throw new ArgumentNullException("processes");
            if (serverData == null) throw new ArgumentNullException("serverData");

            var context = new BackgroundProcessContext(GetGloballyUniqueServerId(), storage, _cts.Token);
            foreach (var item in serverData)
            {
                context.ServerData.Add(item.Key, item.Value);
            }

            Logger.Info("Starting Hangfire Server");

            _bootstrapTask = WrapProcess(new ServerBootstrapper(processes.Select(WrapProcess)))
                .CreateTask(context);
        }

        public TimeSpan ShutdownTimeout { get; set; }

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
            if (_innerServer != null)
            {
                _innerServer.Dispose();
                return;
            }

            _cts.Cancel();

            if (!_bootstrapTask.Wait(ShutdownTimeout))
            {
                Logger.WarnFormat("Hangfire Server takes too long to shutdown. Performing ungraceful shutdown.");
            }
            
            Logger.Info("Hangfire Server stopped.");
        }

        public static BackgroundJobServer StartNew()
        {
            return StartNew(JobStorage.Current);
        }

        public static BackgroundJobServer StartNew([NotNull] JobStorage storage)
        {
            return StartNew(storage, new BackgroundJobServerOptions());
        }

        public static BackgroundJobServer StartNew([NotNull] BackgroundJobServerOptions options)
        {
            return StartNew(JobStorage.Current, options);
        }

        public static BackgroundJobServer StartNew(
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options)
        {
            return StartNew(storage, options, Enumerable.Empty<IBackgroundProcess>());
        }

        public static BackgroundJobServer StartNew(
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options, 
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (options == null) throw new ArgumentNullException("options");
            if (additionalProcesses == null) throw new ArgumentNullException("additionalProcesses");

            var processes = new List<ILongRunningProcess>();
            processes.AddRange(GetDefaultProcesses(options));
            processes.AddRange(storage.GetComponents());
            processes.AddRange(additionalProcesses);

            var serverData = new Dictionary<string, object>
            {
                { "Queues", options.Queues },
                { "WorkerCount", options.WorkerCount }
            };

            Logger.InfoFormat("Using job storage: '{0}'.", storage);

            storage.WriteOptionsToLog(Logger);
            options.WriteToLog(Logger);

            return new BackgroundJobServer(storage, processes, serverData)
            {
                ShutdownTimeout = options.ShutdownTimeout
            };
        }

        public static IEnumerable<ILongRunningProcess> GetDefaultProcesses()
        {
            return GetDefaultProcesses(new BackgroundJobServerOptions());
        }
        
        public static IEnumerable<ILongRunningProcess> GetDefaultProcesses([NotNull] BackgroundJobServerOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");

            var processes = new List<ILongRunningProcess>();

            for (var i = 0; i < options.WorkerCount; i++)
            {
                processes.Add(new Worker(new WorkerContext(options.Queues, i + 1)));
            }

            processes.Add(new ServerHeartbeat());
            processes.Add(new SchedulePoller(options.SchedulePollingInterval));
            processes.Add(new ServerWatchdog(options.ServerWatchdogOptions));
            processes.Add(new RecurringJobScheduler());

            return processes;
        }

        private static ILongRunningProcess WrapProcess(ILongRunningProcess process)
        {
            return new InfiniteLoopProcess(new AutomaticRetryProcess(process));
        }

        private static string GetGloballyUniqueServerId()
        {
            return String.Format(
                "{0}:{1}:{2}",
                Environment.MachineName.ToLowerInvariant(),
                Process.GetCurrentProcess().Id,
                Guid.NewGuid());
        }
    }
}
