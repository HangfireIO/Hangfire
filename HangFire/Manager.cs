using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire
{
    public class Manager : IDisposable
    {
        private readonly int _concurrency;
        private Thread _balancerThread;
        private readonly BlockingCollection<JobDispatcher> _freeDispatchers;

        private readonly ILog _logger = LogManager.GetLogger(typeof (Manager));

        public Manager(int concurrency)
        {
            _concurrency = concurrency;
            _freeDispatchers = new BlockingCollection<JobDispatcher>(
                new ConcurrentQueue<JobDispatcher>(), 
                concurrency);
        }

        public void Start()
        {
            _balancerThread = new Thread(BalanceTasks);
            // TODO: consider making decision between Foreground & Background threads.
            _balancerThread.Start();
        }

        public void Stop()
        {
            
        }
        
        public void Dispose()
        {
        }

        internal void NotifyFreeProcessor(JobDispatcher jobDispatcher)
        {
            _freeDispatchers.Add(jobDispatcher);
        }

        private void BalanceTasks()
        {
            // TODO: consider thread creation exceptions (???)
            var processors = new List<JobDispatcher>(_concurrency);
            for (var i = 0; i < _concurrency; i++)
            {
                var processor = new JobDispatcher(this);
                processor.Start();

                processors.Add(processor);
            }

            _logger.InfoFormat("{0} threads started.", processors.Count);

            // TODO: handle connection exceptions.
            using (var redis = Factory.CreateRedisClient())
            {
                while (true)
                {
                    // First, we need free JobDispatcher that is ready
                    // to process a job.
                    // TODO: use cancellation token after manager stop.
                    var freeProcessor = _freeDispatchers.Take();

                    // TODO: check for race condition.
                    // Second, we need a job to process.
                    var serializedJob = redis.BlockingDequeueItemFromList("queue:default", null);
                    freeProcessor.Process(serializedJob);
                }
            }
        }
    }
}
