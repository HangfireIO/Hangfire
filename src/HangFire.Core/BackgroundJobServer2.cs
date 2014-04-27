using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HangFire.Server;
using HangFire.Server.Components;
using HangFire.Server.Performing;
using HangFire.States;

namespace HangFire
{
    public class BackgroundJobServer2 : IDisposable
    {
        private static readonly TimeSpan ServerShutdownTimeout = TimeSpan.FromSeconds(15);
        private static readonly int DefaultWorkerCount = Environment.ProcessorCount * 5;

        private readonly JobStorage _storage;
        private readonly string _serverId;
        private readonly int _workerCount;
        private readonly string[] _queues;
        private readonly IServerComponentRunner _serverRunner;

        public BackgroundJobServer2(params string[] queues)
            : this(DefaultWorkerCount, queues)
        {
        }

        public BackgroundJobServer2(int workerCount, params string[] queues)
            : this(JobStorage.Current, workerCount, queues)
        {
        }

        public BackgroundJobServer2(JobStorage storage, int workerCount, params string[] queues)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (queues == null) throw new ArgumentNullException("queues");
            if (workerCount <= 0) throw new ArgumentOutOfRangeException("workerCount", "Worker count value must be more than zero.");

            _storage = storage;
            _workerCount = workerCount;
            _queues = queues.Length != 0 ? queues : new[] { EnqueuedState.DefaultQueue };

            _serverId = String.Format("{0}:{1}", Environment.MachineName.ToLowerInvariant(), Process.GetCurrentProcess().Id);

// ReSharper disable once DoNotCallOverridableMethodsInConstructor
            _serverRunner = GetServerRunner();
        }

        public virtual void Start()
        {
            _serverRunner.Start();
        }

        public virtual void Stop()
        {
            _serverRunner.Stop();
        }

        public void Dispose()
        {
            _serverRunner.Dispose();
        }

        internal virtual IServerComponentRunner GetServerRunner()
        {
            var context = new ServerContext
            {
                Queues = _queues,
                WorkerCount = _workerCount
            };

            var server = new JobServer2(
                _serverId, 
                context, 
                _storage, 
                new Lazy<IServerComponentRunner>(GetServerComponentsRunner));

            return new ServerComponentRunner(
                server, 
                new ServerComponentRunnerOptions { ShutdownTimeout = ServerShutdownTimeout });
        }

        internal IServerComponentRunner GetServerComponentsRunner()
        {
            var componentRunners = new List<IServerComponentRunner>();

            componentRunners.AddRange(GetCommonComponentRunners());
            componentRunners.AddRange(GetStorageComponentRunners());

            return new ServerComponentRunnerCollection(componentRunners);
        }

        private IEnumerable<IServerComponentRunner> GetCommonComponentRunners()
        {
            yield return new ServerComponentRunner(
                new ServerHeartbeat(_storage, _serverId));

            yield return new WorkerManager2(
                _serverId, _workerCount, _queues, _storage, new JobPerformanceProcess());

            yield return new ServerComponentRunner(
                new ServerWatchdog2(_storage));
        }

        private IEnumerable<IServerComponentRunner> GetStorageComponentRunners()
        {
            var components = _storage.GetComponents2();

            return components
                .Select(component => new ServerComponentRunner(component))
                .ToArray();
        }
    }
}
