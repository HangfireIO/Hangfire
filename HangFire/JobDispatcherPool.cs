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
            _freeDispatchers = new BlockingCollection<JobDispatcher>(
                new ConcurrentQueue<JobDispatcher>());

            for (var i = 0; i < count; i++)
            {
                var dispatcher = new JobDispatcher(this);
                _dispatchers.Add(dispatcher);
            }
        }

        public void Process(string serializedJob)
        {
            var freeDispatcher = _freeDispatchers.Take();
            freeDispatcher.Process(serializedJob);
        }

        internal void NotifyReady(JobDispatcher dispatcher)
        {
            _freeDispatchers.Add(dispatcher);
        }
    }
}
