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
        public static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(15);

        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly IEnumerable<IServerProcess> _processes;
        private readonly Task _bootstrapTask;

        public BackgroundProcessingServer([NotNull] IEnumerable<IServerProcess> processes)
            : this(JobStorage.Current, processes)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] IEnumerable<IServerProcess> processes,
            [NotNull] IDictionary<string, object> properties)
            : this(JobStorage.Current, processes, properties)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IServerProcess> processes)
            : this(storage, processes, new Dictionary<string, object>())
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage, 
            [NotNull] IEnumerable<IServerProcess> processes,
            [NotNull] IDictionary<string, object> properties)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (processes == null) throw new ArgumentNullException("processes");
            if (properties == null) throw new ArgumentNullException("properties");

            _processes = processes;

            var context = new BackgroundProcessContext(GetGloballyUniqueServerId(), storage, _cts.Token);
            foreach (var item in properties)
            {
                context.Properties.Add(item.Key, item.Value);
            }

            _bootstrapTask = WrapProcess(this).CreateTask(context);

            ShutdownTimeout = DefaultShutdownTimeout;
        }

        public TimeSpan ShutdownTimeout { get; set; }

        public void Dispose()
        {
            _cts.Cancel();

            if (!_bootstrapTask.Wait(ShutdownTimeout))
            {
                Logger.WarnFormat("Hangfire Server takes too long to shutdown. Performing ungraceful shutdown.");
            }
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

        public override string ToString()
        {
            return GetType().Name;
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
