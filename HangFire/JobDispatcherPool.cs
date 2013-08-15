using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HangFire
{
    internal class JobDispatcherPool
    {
        private readonly List<JobDispatcher> _dispatchers;
        private readonly BlockingCollection<JobDispatcher> _freeDispatchers;  

        public JobDispatcherPool(int count)
        {
            _dispatchers = new List<JobDispatcher>(count);
            _freeDispatchers = new BlockingCollection<JobDispatcher>();

            for (var i = 0; i < count; i++)
            {
                var dispatcher = new JobDispatcher(this, String.Format("HangFire.Dispatcher.{0}", i));
                _dispatchers.Add(dispatcher);
            }
        }

        public JobDispatcher TakeFree()
        {
            JobDispatcher dispatcher;
            do
            {
                dispatcher = _freeDispatchers.Take();
            }
            while (dispatcher.IsStopped);

            return dispatcher;
        }

        internal void NotifyReady(JobDispatcher dispatcher)
        {
            _freeDispatchers.Add(dispatcher);
        }
    }
}
