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
#if NETFULL
using System.Diagnostics;
#endif
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;

namespace Hangfire.Server
{
    /// <summary>
    /// Responsible for running the given collection background processes.
    /// </summary>
    /// 
    /// <remarks>
    /// Immediately starts the processes in a background thread.
    /// Responsible for announcing/removing a server, bound to a storage.
    /// Wraps all the processes with a infinite loop and automatic retry.
    /// Executes all the processes in a single context.
    /// Uses timeout in dispose method, waits for all the components, cancel signals shutdown
    /// Contains some required processes and uses storage processes.
    /// Generates unique id.
    /// Properties are still bad.
    /// </remarks>
    public sealed class BackgroundProcessingServer : IBackgroundProcess, IDisposable
    {
        public static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(15);
        private static readonly ILog Logger = LogProvider.For<BackgroundProcessingServer>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
#pragma warning disable 618
        private readonly List<IServerProcess> _processes = new List<IServerProcess>();
#pragma warning restore 618

        private readonly BackgroundProcessingServerOptions _options;
        private readonly Task _bootstrapTask;

        private bool _disposed;

        public BackgroundProcessingServer([NotNull] IEnumerable<IBackgroundProcess> processes)
            : this(JobStorage.Current, processes)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties)
            : this(JobStorage.Current, processes, properties)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes)
            : this(storage, processes, new Dictionary<string, object>())
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties)
            : this(storage, processes, properties, new BackgroundProcessingServerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundProcessingServer"/>
        /// class and immediately starts all the given background processes.
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="processes"></param>
        /// <param name="properties"></param>
        /// <param name="options"></param>
        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties,
            [NotNull] BackgroundProcessingServerOptions options)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (processes == null) throw new ArgumentNullException(nameof(processes));
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _options = options;

            _processes.AddRange(GetRequiredProcesses());
            _processes.AddRange(storage.GetComponents());
            _processes.AddRange(processes);

            var context = new BackgroundProcessContext(
                GetGloballyUniqueServerId(),
                storage,
                properties,
                _cts.Token);

            _bootstrapTask = WrapProcess(this).CreateTask(context);
        }

        public void SendStop()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);

            try
            {
                _cts.Cancel();
            }
            catch (AggregateException ex)
            {
                Logger.WarnException(@"CancellationTokenSource.Cancel() method threw an exception during a server shutdown. 
It can be related to user-defined CancellationToken's callback threw exception. If this isn't your case please contact the Hangfire developers.", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            SendStop();

            // TODO: Dispose _cts

            // Check ShutdownTimeout value. If it can cause an exception use default value.
            var shutdownTimeout = _options.ShutdownTimeout;

            if ((shutdownTimeout < TimeSpan.Zero && shutdownTimeout != Timeout.InfiniteTimeSpan) ||
                shutdownTimeout.TotalMilliseconds > Int32.MaxValue)
            {
                Logger.Warn($@"ShutdownTimeout equals {_options.ShutdownTimeout.Milliseconds} milliseconds and it't incorret. 
This value must be either equal to or less than {Int32.MaxValue} milliseconds and non-negative or infinite.
Will be used default value: {DefaultShutdownTimeout} milliseconds");

                shutdownTimeout = DefaultShutdownTimeout;
            }

            if (!_bootstrapTask.Wait(shutdownTimeout))
            {
                Logger.Warn("Processing server takes too long to shutdown. Performing ungraceful shutdown.");
            }

            _disposed = true;
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        void IBackgroundProcess.Execute(BackgroundProcessContext context)
        {
            using (var connection = context.Storage.GetConnection())
            {
                var serverContext = GetServerContext(context.Properties);
                connection.AnnounceServer(context.ServerId, serverContext);
            }

            try
            {
                var tasks = _processes
                    .Select(WrapProcess)
                    .Select(process => process.CreateTask(context))
                    .ToArray();

                Task.WaitAll(tasks);
            }
            finally
            {
                try
                {
                    using (var connection = context.Storage.GetConnection())
                    {
                        connection.RemoveServer(context.ServerId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WarnException($@"Couldn't remove server {context.ServerId}. The server can be displayed on 'Server' page of Dashboard for a while. 
The server won't perform any jobs and won't affect other servers.", ex);
                }
            }
        }

        private IEnumerable<IBackgroundProcess> GetRequiredProcesses()
        {
            yield return new ServerHeartbeat(_options.HeartbeatInterval);
            yield return new ServerWatchdog(_options.ServerCheckInterval, _options.ServerTimeout);
        }

        private string GetGloballyUniqueServerId()
        {
            var serverName = _options.ServerName
                ?? Environment.GetEnvironmentVariable("COMPUTERNAME")
                ?? Environment.GetEnvironmentVariable("HOSTNAME");

            var guid = Guid.NewGuid().ToString();

#if NETFULL
            if (!String.IsNullOrWhiteSpace(serverName))
            {
                serverName += ":" + Process.GetCurrentProcess().Id;
            }
#endif

            return !String.IsNullOrWhiteSpace(serverName)
                ? $"{serverName.ToLowerInvariant()}:{guid}"
                : guid;
        }

#pragma warning disable 618
        private static IServerProcess WrapProcess(IServerProcess process)
#pragma warning restore 618
        {
            return new InfiniteLoopProcess(new AutomaticRetryProcess(process));
        }

        private static ServerContext GetServerContext(IReadOnlyDictionary<string, object> properties)
        {
            var serverContext = new ServerContext();

            if (properties.ContainsKey("Queues"))
            {
                var array = properties["Queues"] as string[];
                if (array != null)
                {
                    serverContext.Queues = array;
                }
            }

            if (properties.ContainsKey("WorkerCount"))
            {
                serverContext.WorkerCount = (int)properties["WorkerCount"];
            }
            return serverContext;
        }
    }
}
