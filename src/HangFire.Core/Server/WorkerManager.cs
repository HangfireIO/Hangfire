using System;
using System.Collections.Generic;
using HangFire.Server.Performing;

namespace HangFire.Server
{
    internal class WorkerManager : IServerComponentRunner
    {
        private readonly JobStorage _storage;
        private readonly IJobPerformanceProcess _performanceProcess;
        private readonly ServerComponentRunnerCollection _workerRunners;

        public WorkerManager(
            string serverId,
            int workerCount, 
            string[] queues,
            JobStorage storage, 
            IJobPerformanceProcess performanceProcess)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (storage == null) throw new ArgumentNullException("storage");
            if (performanceProcess == null) throw new ArgumentNullException("performanceProcess");
            if (queues == null) throw new ArgumentNullException("queues");
            if (workerCount <= 0) throw new ArgumentOutOfRangeException("workerCount", "Worker count value must be more than zero.");

            _storage = storage;
            _performanceProcess = performanceProcess;

            var workerRunners = new List<IServerComponentRunner>(workerCount);
            for (var i = 1; i <= workerCount; i++)
            {
                var workerContext = new WorkerContext(serverId, queues, i);

// ReSharper disable once DoNotCallOverridableMethodsInConstructor
                workerRunners.Add(CreateWorkerRunner(workerContext));
            }

            _workerRunners = new ServerComponentRunnerCollection(workerRunners);
        }

        public void Start()
        {
            _workerRunners.Start();
        }

        public void Stop()
        {
            _workerRunners.Stop();
        }

        public void Dispose()
        {
            _workerRunners.Dispose();
        }

        internal virtual IServerComponentRunner CreateWorkerRunner(WorkerContext context)
        {
            return new ServerComponentRunner(
                new Worker(_storage, context, _performanceProcess),
                new ServerComponentRunnerOptions { MinimumLogVerbosity = true });
        }
    }
}
