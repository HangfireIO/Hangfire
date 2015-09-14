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
using Hangfire.Annotations;
using Hangfire.Logging;

namespace Hangfire.Server
{
    public sealed class BackgroundProcessingServer : IBackgroundProcess, IDisposable
    {
        private readonly BackgroundProcessingServerOptions _options;
        public static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(15);

        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly List<IServerProcess> _processes = new List<IServerProcess>();
        private readonly Task _bootstrapTask;

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

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage, 
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties, 
            [NotNull] BackgroundProcessingServerOptions options)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (processes == null) throw new ArgumentNullException("processes");
            if (properties == null) throw new ArgumentNullException("properties");
            if (options == null) throw new ArgumentNullException("options");

            _options = options;

            _processes.AddRange(GetRequiredProcesses());
            _processes.AddRange(storage.GetComponents());
            _processes.AddRange(processes);

            var context = new BackgroundProcessContext(GetGloballyUniqueServerId(), storage, _cts.Token);
            foreach (var item in properties)
            {
                context.Properties.Add(item.Key, item.Value);
            }

            _bootstrapTask = WrapProcess(this).CreateTask(context);
        }

        public void Dispose()
        {
            _cts.Cancel();

            if (!_bootstrapTask.Wait(_options.ShutdownTimeout))
            {
                Logger.WarnFormat("Processing server takes too long to shutdown. Performing ungraceful shutdown.");
            }
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
                using (var connection = context.Storage.GetConnection())
                {
                    connection.RemoveServer(context.ServerId);
                }
            }
        }

        private IEnumerable<IBackgroundProcess> GetRequiredProcesses()
        {
            yield return new ServerHeartbeat(_options.HeartbeatInterval);
            yield return new ServerWatchdog(_options.ServerCheckInterval, _options.ServerTimeout);
        } 

        private static IServerProcess WrapProcess(IServerProcess process)
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

        private static ServerContext GetServerContext(IDictionary<string, object> properties)
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
