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
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire
{
    public class BackgroundJobServer : IDisposable
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly BackgroundJobServerOptions _options;
        private readonly IDisposable _server;

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
        public BackgroundJobServer([NotNull] JobStorage storage)
            : this(new BackgroundJobServerOptions(), storage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class
        /// with the given options and <see cref="JobStorage.Current"/> storage.
        /// </summary>
        /// <param name="options">Server options</param>
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
        public BackgroundJobServer([NotNull] BackgroundJobServerOptions options, [NotNull] JobStorage storage)
            : this(options, storage, Enumerable.Empty<IBackgroundProcess>())
        {
        }

        public BackgroundJobServer(
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (options == null) throw new ArgumentNullException("options");
            if (additionalProcesses == null) throw new ArgumentNullException("additionalProcesses");

            _options = options;

            var processes = new List<IServerProcess>();
            processes.AddRange(GetProcesses());
            processes.AddRange(storage.GetComponents());
            processes.AddRange(additionalProcesses);

            var properties = new Dictionary<string, object>
            {
                { "Queues", options.Queues },
                { "WorkerCount", options.WorkerCount }
            };

            Logger.Info("Starting Hangfire Server");
            Logger.InfoFormat("Using job storage: '{0}'.", storage);

            storage.WriteOptionsToLog(Logger);
            options.WriteToLog(Logger);

            _server = new BackgroundServer(storage, processes, properties)
            {
                ShutdownTimeout = options.ShutdownTimeout
            };
        }

        public void Dispose()
        {
            _server.Dispose();
            Logger.Info("Hangfire Server stopped.");
        }

        private IEnumerable<IServerProcess> GetProcesses()
        {
            var processes = new List<IServerProcess>();

            for (var i = 0; i < _options.WorkerCount; i++)
            {
                processes.Add(new Worker(new WorkerContext(_options.Queues, (i + 1).ToString()), _options.PerformanceProcess, _options.StateChangeProcess));
            }

            processes.Add(new ServerHeartbeat(_options.HeartbeatInterval));
            processes.Add(new ServerWatchdog(_options.ServerWatchdogOptions));
            processes.Add(new SchedulePoller(_options.SchedulePollingInterval, _options.StateChangeProcess));
            processes.Add(new RecurringJobScheduler(_options.CreationProcess));

            return processes;
        }

        [Obsolete("This method is a stub. There is no need to call the `Start` method. Will be removed in version 2.0.0.")]
        public void Start()
        {
        }

        [Obsolete("This method is a stub. Please call the `Dispose` method instead. Will be removed in version 2.0.0.")]
        public void Stop()
        {
        }
    }
}
