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
        private readonly ILog _log = LogManager.GetLogger("HangFire.JobDispatcherPool");

        public JobDispatcherPool(int count)
        {
            _dispatchers = new List<JobDispatcher>(count);
            _freeDispatchers = new BlockingCollection<JobDispatcher>();

            for (var i = 0; i < count; i++)
            {
                var dispatcher = new JobDispatcher(this, String.Format("HangFire.Dispatcher.{0}", i));
                dispatcher.Start();
                _dispatchers.Add(dispatcher);
            }
        }

        public JobDispatcher TakeFree(CancellationToken cancellationToken)
        {
            JobDispatcher dispatcher;
            do
            {
                dispatcher = _freeDispatchers.Take(cancellationToken);
            }
            while (dispatcher.Crashed);

            return dispatcher;
        }

        public void Dispose()
        {
            _log.Info("Stopping dispatchers...");
            foreach (var dispatcher in _dispatchers)
            {
                dispatcher.Stop();
            }

            foreach (var dispatcher in _dispatchers)
            {
                dispatcher.Dispose();
            }
            _log.Info("Dispatchers were stopped.");
        }

        internal void NotifyReady(JobDispatcher dispatcher)
        {
            _freeDispatchers.Add(dispatcher);
        }
    }
}
