using System;
using System.Collections.Concurrent;
using System.Threading;

namespace HangFire
{
    internal class JobFetcher
    {
        private readonly Thread _jobCompletionHandlerThread;

        private readonly BlockingCollection<Tuple<string, Exception>> _completed
            = new BlockingCollection<Tuple<string, Exception>>();

        public JobFetcher()
        {
            _jobCompletionHandlerThread = new Thread(HandleJobCompletion) { IsBackground = true };
        }

        public void Start()
        {
            _jobCompletionHandlerThread.Start();
        }

        public string TakeNext()
        {
            // TODO: handle redis exceptions
            using (var redis = Factory.CreateRedisClient())
            {
                return redis.BlockingDequeueItemFromList("queue:default", null);
            }
        }

        public void Process(Tuple<string, Exception> e)
        {
            _completed.Add(e);
        }

        private void HandleJobCompletion()
        {
            while (true)
            {
                var completedJob = _completed.Take();
                if (completedJob.Item2 != null)
                {
                    // TODO: handle redis exceptions
                    using (var redis = Factory.CreateRedisClient())
                    {
                        redis.EnqueueItemOnList("jobs:failed", completedJob.Item1);
                    }
                }
            }
        }
    }
}
