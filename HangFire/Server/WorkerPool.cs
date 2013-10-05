using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using ServiceStack.Logging;

namespace HangFire.Server
{
    internal class WorkerPool : IDisposable
    {
        private readonly List<Worker> _workers;
        private readonly BlockingCollection<Worker> _freeWorkers;
        private readonly ILog _logger = LogManager.GetLogger("HangFire.WorkerPool");
        private bool _disposed;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public WorkerPool(
            ServerContext serverContext,
            ServerJobInvoker jobInvoker,
            JobActivator jobActivator)
        {
            _workers = new List<Worker>(serverContext.WorkersCount);
            _freeWorkers = new BlockingCollection<Worker>();

            _logger.Info(String.Format("Starting {0} workers...", serverContext.WorkersCount));

            for (var i = 0; i < serverContext.WorkersCount; i++)
            {
                var worker = new Worker(
                    this,
                    serverContext,
                    i,
                    jobInvoker,
                    jobActivator);

                worker.Start();
                _workers.Add(worker);
            }

            _logger.Info("Workers were started.");
        }

        public event EventHandler<JobCompletedEventArgs> JobCompleted;

        public Worker TakeFree(CancellationToken cancellationToken)
        {
            Debug.Assert(!_disposed, "!_disposed");

            Worker worker;
            do
            {
                worker = _freeWorkers.Take(cancellationToken);
            }
            while (worker.Crashed);

            return worker;
        }

        internal void NotifyCompleted(string jobId)
        {
            var onCompleted = JobCompleted;
            if (onCompleted != null)
            {
                onCompleted(this, new JobCompletedEventArgs(jobId));
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _logger.Info("Stopping workers...");
            foreach (var worker in _workers)
            {
                worker.Stop();
            }

            foreach (var worker in _workers)
            {
                worker.Dispose();
            }
            _logger.Info("Workers were stopped.");

            _freeWorkers.Dispose();
        }

        internal void NotifyReady(Worker worker)
        {
            _freeWorkers.Add(worker);
        }
    }
}
