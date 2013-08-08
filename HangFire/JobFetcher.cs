using System;
using System.Collections.Concurrent;
using System.Threading;

namespace HangFire
{
    internal class JobFetcher
    {
        private readonly Thread _fetcherThread;
        private readonly Thread _jobCompletionHandlerThread;

        private readonly BlockingCollection<Tuple<string, Exception>> _completed
            = new BlockingCollection<Tuple<string, Exception>>();

        public JobFetcher()
        {
            _fetcherThread = new Thread(FetchNextTask) { IsBackground = true };
            _jobCompletionHandlerThread = new Thread(HandleJobCompletion) { IsBackground = true };
        }

        public event EventHandler<string> JobFetched; 

        public void Start()
        {
            _fetcherThread.Start();
            _jobCompletionHandlerThread.Start();
        }

        public void Process(Tuple<string, Exception> e)
        {
            _completed.Add(e);
        }

        private void FetchNextTask()
        {
            // TODO: handle connection exceptions.
            using (var redis = Factory.CreateRedisClient())
            {
                while (true)
                {
                    var serializedJob = redis.BlockingDequeueItemFromList("queue:default", null);
                    var onFetched = JobFetched;
                    if (onFetched != null)
                    {
                        onFetched(this, serializedJob);
                    }
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
    }
}
