using System;
using ServiceStack.Redis;

namespace HangFire.Storage.Redis
{
    public class RedisStorageConnection : IStorageConnection
    {
        private const string Prefix = "hangfire:";
        private readonly IRedisClient _redis;

        public RedisStorageConnection(IRedisClient redis)
        {
            _redis = redis;
            var storage = new RedisStorage(redis);
            Jobs = storage;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        public IAtomicWriteTransaction CreateWriteTransaction()
        {
            return new RedisAtomicWriteTransaction(_redis.CreateTransaction());
        }

        public IDisposable AcquireLock(string resource, TimeSpan timeOut)
        {
            return _redis.AcquireLock(Prefix + resource, timeOut);
        }

        public IStoredJobs Jobs { get; private set; }
    }
}