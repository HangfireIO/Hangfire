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
        private readonly BlockingCollection<Processor> _freeProcessors;

        private readonly ILog _logger = LogManager.GetLogger(typeof (Manager));

        public Manager(int concurrency)
        {
            _concurrency = concurrency;
            _freeProcessors = new BlockingCollection<Processor>(
                new ConcurrentQueue<Processor>(), 
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

        internal void NotifyFreeProcessor(Processor processor)
        {
            _freeProcessors.Add(processor);
        }

        private void BalanceTasks()
        {
            // TODO: consider thread creation exceptions (???)
            var processors = new List<Processor>(_concurrency);
            for (var i = 0; i < _concurrency; i++)
            {
                var processor = new Processor(this);
                processor.Start();

                processors.Add(processor);
            }

            _logger.InfoFormat("{0} threads started.", processors.Count);

            using (var redis = Factory.CreateRedisClient())
            {
                while (true)
                {
                    // First, we need free processor that is ready
                    // to process a job.
                    // TODO: use cancellation token after manager stop.
                    var freeProcessor = _freeProcessors.Take();

                    // TODO: check for race condition.
                    // Second, we need a job to process.
                    var serializedJob = redis.BlockingDequeueItemFromList("queue:default", null);
                    freeProcessor.Process(serializedJob);
                }
            }
        }
    }
}
