using System;
using System.Linq;
using ServiceStack.Redis;

namespace HangFire
{
    public class RedisStorage
    {
        private readonly IRedisClient _redis;

        public RedisStorage(IRedisClient redis)
        {
            _redis = redis;
        }

        public void ScheduleJob(string job, double at)
        {
            _redis.AddItemToSortedSet("hangfire:schedule", job, at);
        }

        public string GetScheduledJob(double now)
        {
            var scheduledJob =
                _redis.GetRangeFromSortedSetByLowestScore("hangfire:schedule", Double.NegativeInfinity, now, 0, 1)
                    .FirstOrDefault();

            if (scheduledJob != null)
            {
                if (_redis.RemoveItemFromSortedSet("hangfire:schedule", scheduledJob))
                {
                    return scheduledJob;
                }    
            }

            return null;
        }

        public void EnqueueJob(string job)
        {
            _redis.EnqueueItemOnList("hangfire:queue:default", job);
        }

        public string DequeueJob()
        {
            return _redis.BlockingDequeueItemFromList(
                "hangfire:queue:default", TimeSpan.FromSeconds(1));
        }
    }
}
