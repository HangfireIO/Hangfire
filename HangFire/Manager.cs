using System;
using System.Collections.Concurrent;
using System.Threading;

namespace HangFire
{
    public class Manager
    {
        private readonly JobDispatcherPool _pool;
        private readonly Thread _fetcherThread;
        private readonly Thread _jobCompletionHandlerThread;

        private readonly BlockingCollection<Tuple<string, Exception>> _completed
            = new BlockingCollection<Tuple<string, Exception>>();  

        public Manager(int concurrency)
        {
            _pool = new JobDispatcherPool(concurrency);
            _pool.JobCompleted += PoolOnJobCompleted;

            _fetcherThread = new Thread(FetchNextTask) { IsBackground = true };
            _jobCompletionHandlerThread = new Thread(HandleJobCompletion) { IsBackground = true };
        }

        public void Start()
        {
            _fetcherThread.Start();
            _jobCompletionHandlerThread.Start();
        }

        private void FetchNextTask()
        {
            // TODO: handle connection exceptions.
            using (var redis = Factory.CreateRedisClient())
            {
                while (true)
                {
                    var serializedJob = redis.BlockingDequeueItemFromList("queue:default", null);
                    _pool.Process(serializedJob);
                }
            }
        }

        private void HandleJobCompletion()
        {
            // TODO: Handle connection exceptions
            using (var redis = Factory.CreateRedisClient())
            {
                while (true)
                {
                    var completedJob = _completed.Take();
                    if (completedJob.Item2 != null)
                    {
                        redis.EnqueueItemOnList("jobs:failed", completedJob.Item1);
                    }
                }
            }
        }

        private void PoolOnJobCompleted(object sender, Tuple<string, Exception> e)
        {
            _completed.Add(e);
        }
    }
}
