using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire
{
    internal class ThreadedWorkerManager : IDisposable
    {
        private readonly List<ThreadedWorker> _workers;
        private readonly BlockingCollection<ThreadedWorker> _freeWorkers;
        private readonly ILog _logger = LogManager.GetLogger("HangFire.WorkerPool");
        private bool _disposed;

        public ThreadedWorkerManager(int count, string serverName, HangFireJobActivator jobActivator)
        {
            _workers = new List<ThreadedWorker>(count);
            _freeWorkers = new BlockingCollection<ThreadedWorker>();

            _logger.Info(String.Format("Starting {0} workers...", count));

            for (var i = 0; i < count; i++)
            {
                var worker = new ThreadedWorker(
                    this, 
                    String.Format("HangFire.Worker.{0}", i),
                    String.Format("{0}.{1}", serverName, i),
                    jobActivator);
                worker.Start();
                _workers.Add(worker);
            }

            _logger.Info("Workers were started.");
        }

        public event EventHandler<JobCompletedEventArgs> JobCompleted;

        public ThreadedWorker TakeFree(CancellationToken cancellationToken)
        {
            Debug.Assert(!_disposed, "!_disposed");

            ThreadedWorker worker;
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

        internal void NotifyReady(ThreadedWorker worker)
        {
            _freeWorkers.Add(worker);
        }
    }
}
