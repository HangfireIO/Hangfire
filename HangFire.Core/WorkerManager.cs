using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire
{
    internal class WorkerManager : IDisposable
    {
        private readonly List<Worker> _dispatchers;
        private readonly BlockingCollection<Worker> _freeDispatchers;
        private readonly ILog _logger = LogManager.GetLogger("HangFire.JobDispatcherPool");
        private bool _disposed;

        public WorkerManager(int count, string serverName, HangFireJobActivator jobActivator)
        {
            _dispatchers = new List<Worker>(count);
            _freeDispatchers = new BlockingCollection<Worker>();

            _logger.Info(String.Format("Starting {0} dispatchers...", count));

            for (var i = 0; i < count; i++)
            {
                var dispatcher = new Worker(
                    this, 
                    String.Format("HangFire.Dispatcher.{0}", i),
                    String.Format("{0}.{1}", serverName, i),
                    jobActivator);
                dispatcher.Start();
                _dispatchers.Add(dispatcher);
            }

            _logger.Info("Dispatchers were started.");
        }

        public event EventHandler<JobCompletedEventArgs> JobCompleted;

        public Worker TakeFree(CancellationToken cancellationToken)
        {
            Debug.Assert(!_disposed, "!_disposed");

            Worker dispatcher;
            do
            {
                dispatcher = _freeDispatchers.Take(cancellationToken);
            }
            while (dispatcher.Crashed);

            return dispatcher;
        }

        internal void NotifyCompleted(string job)
        {
            var onCompleted = JobCompleted;
            if (onCompleted != null)
            {
                onCompleted(this, new JobCompletedEventArgs(job));
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _logger.Info("Stopping dispatchers...");
            foreach (var dispatcher in _dispatchers)
            {
                dispatcher.Stop();
            }

            foreach (var dispatcher in _dispatchers)
            {
                dispatcher.Dispose();
            }
            _logger.Info("Dispatchers were stopped.");

            _freeDispatchers.Dispose();
        }

        internal void NotifyReady(Worker dispatcher)
        {
            _freeDispatchers.Add(dispatcher);
        }
    }
}
