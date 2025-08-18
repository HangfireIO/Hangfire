// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire
{
    public class BackgroundJobServer : IBackgroundProcessingServer
    {
        private readonly ILog _logger;

        private readonly BackgroundJobServerOptions _options;
        private readonly BackgroundProcessingServer _processingServer;

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
            : this(options, storage, additionalProcesses, null, null, null)
        {
        }

        public BackgroundJobServer(
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses,
            [CanBeNull] IBackgroundJobFactory? factory,
            [CanBeNull] IBackgroundJobPerformer? performer,
            [CanBeNull] IBackgroundJobStateChanger? stateChanger)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (additionalProcesses == null) throw new ArgumentNullException(nameof(additionalProcesses));

            _options = options;
            var processes = new List<IBackgroundProcessDispatcherBuilder>();
            processes.AddRange(GetRequiredProcesses(factory, performer, stateChanger));
            processes.AddRange(additionalProcesses.Select(static x => x.UseBackgroundPool(1)));

            var properties = new Dictionary<string, object>
            {
                { "Queues", options.Queues },
                { "WorkerCount", options.WorkerCount }
            };

            var logProvider = options.LogProvider ?? LogProvider.GetCurrentLogProvider();
            _logger = logProvider.GetLogger(typeof(BackgroundJobServer).FullName!); // TODO: Get wrapped logger instead

            _logger.Info($"Starting Hangfire Server using job storage: '{storage}'");

            storage.WriteOptionsToLog(_logger);

            _logger.Info("Using the following options for Hangfire Server:\r\n" +
                $"    Worker count: {options.WorkerCount}\r\n" +
                $"    Listening queues: {String.Join(", ", options.Queues.Select(static x => "'" + x + "'"))}\r\n" +
                $"    Shutdown timeout: {options.ShutdownTimeout}\r\n" +
                $"    Schedule polling interval: {options.SchedulePollingInterval}");

            var wrongQueues = new HashSet<string>(StringComparer.Ordinal);
            foreach (var queue in options.Queues)
            {
                if (!EnqueuedState.TryValidateQueueName(queue))
                {
                    wrongQueues.Add(queue);
                }
            }

            if (wrongQueues.Count > 0)
            {
                _logger.Warn($"These queues fail to match the naming format: {String.Join(", ", wrongQueues.Select(static x => $"'{x}'"))}. A queue name must consist of lowercase letters, digits, underscore, and dash characters only.");
            }

            _processingServer = new BackgroundProcessingServer(
                storage, 
                processes, 
                properties, 
                GetProcessingServerOptions(),
                logProvider);
        }

        [Obsolete("Create your own BackgroundJobServer-like type and pass custom services to it. This constructor will be removed in 2.0.0.")]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public BackgroundJobServer(
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses,
            [CanBeNull] IJobFilterProvider? filterProvider,
            [CanBeNull] JobActivator? activator,
            [CanBeNull] IBackgroundJobFactory? factory,
            [CanBeNull] IBackgroundJobPerformer? performer,
            [CanBeNull] IBackgroundJobStateChanger? stateChanger)
            : this(options.CloneWithFilterAndActivator(filterProvider, activator), storage, additionalProcesses, factory, performer, stateChanger)
        {
        }

        public void SendStop()
        {
            _logger.Debug("Hangfire Server is stopping...");
            _processingServer.SendStop();
        }

        public void Dispose()
        {
            _processingServer.Dispose();
            GC.SuppressFinalize(this);
        }

        [Obsolete("This method is a stub. There is no need to call the `Start` method. Will be removed in version 2.0.0.")]
        public void Start()
        {
        }

        [Obsolete("Please call the `Shutdown` method instead. Will be removed in version 2.0.0.")]
        public void Stop()
        {
            SendStop();
        }

        [Obsolete("Please call the `Shutdown` method instead. Will be removed in version 2.0.0.")]
        public void Stop(bool force)
        {
            SendStop();
        }

        public bool WaitForShutdown(TimeSpan timeout)
        {
            return _processingServer.WaitForShutdown(timeout);
        }

        public Task WaitForShutdownAsync(CancellationToken cancellationToken)
        {
            return _processingServer.WaitForShutdownAsync(cancellationToken);
        }

        private IEnumerable<IBackgroundProcessDispatcherBuilder> GetRequiredProcesses(
            IBackgroundJobFactory? factory,
            IBackgroundJobPerformer? performer,
            IBackgroundJobStateChanger? stateChanger)
        {
            var processes = new List<IBackgroundProcessDispatcherBuilder>();
            var timeZoneResolver = _options.TimeZoneResolver ?? new DefaultTimeZoneResolver();

            if (factory == null && performer == null && stateChanger == null)
            {
                var filterProvider = _options.FilterProvider ?? JobFilterProviders.Providers;
                var activator = _options.Activator ?? JobActivator.Current;

                factory = new BackgroundJobFactory(filterProvider);
                performer = new BackgroundJobPerformer(filterProvider, activator, _options.TaskScheduler);
                stateChanger = new BackgroundJobStateChanger(filterProvider);
            }
            else
            {
                if (factory == null) throw new ArgumentNullException(nameof(factory));
                if (performer == null) throw new ArgumentNullException(nameof(performer));
                if (stateChanger == null) throw new ArgumentNullException(nameof(stateChanger));
            }

            processes.Add(new Worker(_options.Queues, performer, stateChanger).UseBackgroundPool(_options.WorkerCount, _options.WorkerThreadConfigurationAction));

            if (!_options.IsLightweightServer)
            {
                processes.Add(
                    new DelayedJobScheduler(_options.SchedulePollingInterval, stateChanger)
                    {
                        TaskScheduler = _options.TaskScheduler,
                        MaxDegreeOfParallelism = _options.MaxDegreeOfParallelismForSchedulers
                    }
                    .UseBackgroundPool(1));

                processes.Add(
                    new RecurringJobScheduler(factory, _options.SchedulePollingInterval, timeZoneResolver)
                        {
                            TaskScheduler = _options.TaskScheduler,
                            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelismForSchedulers
                        }
                        .UseBackgroundPool(1));
            }

            return processes;
        }

        private BackgroundProcessingServerOptions GetProcessingServerOptions()
        {
            return new BackgroundProcessingServerOptions
            {
                StopTimeout = _options.StopTimeout,
                ShutdownTimeout = _options.ShutdownTimeout,
                HeartbeatInterval = _options.HeartbeatInterval,
#pragma warning disable 618
                ServerCheckInterval = _options.ServerWatchdogOptions?.CheckInterval ?? _options.ServerCheckInterval,
                ServerTimeout = _options.ServerWatchdogOptions?.ServerTimeout ?? _options.ServerTimeout,
#pragma warning restore 618
                CancellationCheckInterval = _options.CancellationCheckInterval,
                ServerName = _options.ServerName,
                ExcludeStorageProcesses = _options.IsLightweightServer
            };
        }
    }
}
