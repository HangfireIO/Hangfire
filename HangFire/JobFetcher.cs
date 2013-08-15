using System;
using BookSleeve;

namespace HangFire
{
    internal class JobFetcher : IDisposable
    {
        // TODO: handle redis exceptions
        // TODO: handle connection exceptions
        private readonly RedisConnection _blockingRedis = RedisClient.CreateConnection();
        private readonly RedisConnection _redis = RedisClient.CreateConnection();

        public string TakeNext()
        {
            var result = _blockingRedis.Lists
                .BlockingRemoveLastString(0, new[] { "queue:default" }, 0);

            return _blockingRedis.Wait(result).Item2;
        }

        public void AddToFailedQueue(string job)
        {
            _redis.Lists.AddFirst(0, "jobs:failed", job);
        }

        public void Dispose()
        {
            _blockingRedis.Dispose();
            _redis.Dispose();
        }
    }
}
