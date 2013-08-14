using System;

namespace HangFire
{
    internal class JobFetcher
    {
        public string TakeNext()
        {
            // TODO: handle redis exceptions
            using (var redis = Factory.CreateRedisClient())
            {
                return redis.BlockingDequeueItemFromList("queue:default", null);
            }
        }

        public void AddToFailedQueue(string job)
        {
            // TODO: handle redis exceptions
            using (var redis = Factory.CreateRedisClient())
            {
                redis.EnqueueItemOnList("jobs:failed", job);
            }
        }
    }
}
