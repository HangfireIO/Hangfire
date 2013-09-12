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
        private readonly List<ThreadedWorker> _dispatchers;
        private readonly BlockingCollection<ThreadedWorker> _freeDispatchers;
        private readonly ILog _logger = LogManager.GetLogger("HangFire.JobDispatcherPool");
        private bool _disposed;

        public ThreadedWorkerManager(int count, string serverName, HangFireJobActivator jobActivator)
        {
            _dispatchers = new List<ThreadedWorker>(count);
            _freeDispatchers = new BlockingCollection<ThreadedWorker>();

            _logger.Info(String.Format("Starting {0} dispatchers...", count));

            for (var i = 0; i < count; i++)
            {
                var dispatcher = new ThreadedWorker(
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

        public ThreadedWorker TakeFree(CancellationToken cancellationToken)
        {
            Debug.Assert(!_disposed, "!_disposed");

            ThreadedWorker dispatcher;
            do
            {
                dispatcher = _freeDispatchers.Take(cancellationToken);
            }
            while (dispatcher.Crashed);

            return dispatcher;
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

        internal void NotifyReady(ThreadedWorker dispatcher)
        {
            _freeDispatchers.Add(dispatcher);
        }
    }
}
