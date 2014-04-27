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
        private static readonly int DefaultWorkerCount = Environment.ProcessorCount * 5;

        private readonly JobStorage _storage;
        private readonly string _serverId;
        private JobServer2 _server;

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
        }

        public string[] Queues { get; private set; }
        public int WorkerCount { get; private set; }

        public virtual void Start()
        {
            var context = new ServerContext
            {
                Queues = Queues,
                WorkerCount = WorkerCount
            };

            _server = new JobServer2(_serverId, context, _storage, GetServerComponentsRunner());
            _server.Start();
        }

        private IServerComponentRunner GetServerRunner()
        {
            
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
                new ServerComponentRunnerOptions { MinimumLogVerbosity = true }));

            var workerRunnerCollection = new ServerComponentRunnerCollection(workerRunners);

            var components = _storage.GetComponents2();

            var runners = new List<IServerComponentRunner>();
            runners.Add(workerRunnerCollection);
            runners.AddRange(components.Select(component => new ServerComponentRunner(component)));

            return new ServerComponentRunnerCollection(runners);
        }

        public virtual void Stop()
        {
            _server.Stop();
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
