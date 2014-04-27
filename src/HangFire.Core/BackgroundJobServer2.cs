using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HangFire.Server;
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
            _serverId = String.Format("{0}:{1}", Environment.MachineName.ToLowerInvariant(), Process.GetCurrentProcess().Id);

            WorkerCount = workerCount;
            Queues = queues.Length != 0 ? queues : new[] { EnqueuedState.DefaultQueue };

            _serverRunner = GetServerRunner();
        }

        public string[] Queues { get; private set; }
        public int WorkerCount { get; private set; }

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

        private IServerComponentRunner GetServerRunner()
        {
            var context = new ServerContext
            {
                Queues = Queues,
                WorkerCount = WorkerCount
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

        private IServerComponentRunner GetServerComponentsRunner()
        {
            var workers = new List<IServerComponent>(WorkerCount);
            var performanceProcess = new JobPerformanceProcess();
            for (var i = 1; i <= WorkerCount; i++)
            {
                var workerContext = new WorkerContext(_serverId, Queues, i);
                var worker = new Worker2(_storage, workerContext, performanceProcess);

                workers.Add(worker);
            }

            var workerRunners = workers.Select(worker => new ServerComponentRunner(
                worker,
                new ServerComponentRunnerOptions { MinimumLogVerbosity = true })).ToArray();

            var workerRunnerCollection = new ServerComponentRunnerCollection(workerRunners);

            var components = _storage.GetComponents2();

            var runners = new List<IServerComponentRunner>();
            runners.Add(workerRunnerCollection);
            runners.AddRange(components.Select(component => new ServerComponentRunner(component)).ToArray());
            runners.Add(new ServerComponentRunner(new ServerHeartbeat(_storage, _serverId)));

            return new ServerComponentRunnerCollection(runners);
        }
    }
}
