using System;
using HangFire.Server;
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

        public static void RemoveFromDequeuedList(
            IRedisClient redis,
            string queue,
            string jobId)
        {
            using (var transaction = redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromList(
                    String.Format("hangfire:queue:{0}:dequeued", queue),
                    jobId,
                    -1));

                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Fetched"));
                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Checked"));

                transaction.Commit();
            }
        }
    }
}