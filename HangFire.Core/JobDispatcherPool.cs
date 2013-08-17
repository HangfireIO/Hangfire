using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire
{
    internal class JobDispatcherPool : IDisposable
    {
        private readonly List<JobDispatcher> _dispatchers;
        private readonly BlockingCollection<JobDispatcher> _freeDispatchers;
        private readonly ILog _logger = LogManager.GetLogger("HangFire.JobDispatcherPool");
        private bool _disposed;

        public JobDispatcherPool(int count)
        {
            _dispatchers = new List<JobDispatcher>(count);
            _freeDispatchers = new BlockingCollection<JobDispatcher>();

            _logger.Info(String.Format("Starting {0} dispatchers...", count));

            for (var i = 0; i < count; i++)
            {
                var dispatcher = new JobDispatcher(this, String.Format("HangFire.Dispatcher.{0}", i));
                dispatcher.Start();
                _dispatchers.Add(dispatcher);
            }

            _logger.Info("Dispatchers were started.");
        }

        public event EventHandler<string> JobCompleted;

        public JobDispatcher TakeFree(CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            JobDispatcher dispatcher;
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
                onCompleted(this, job);
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

        internal void NotifyReady(JobDispatcher dispatcher)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);

            _freeDispatchers.Add(dispatcher);
        }
    }
}
