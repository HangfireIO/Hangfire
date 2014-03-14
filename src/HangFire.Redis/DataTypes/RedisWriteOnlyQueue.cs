using System;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis.DataTypes
{
    internal class RedisWriteOnlyQueue : IWriteOnlyPersistentQueue
    {
        private readonly IRedisTransaction _transaction;

        public RedisWriteOnlyQueue(IRedisTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void Enqueue(string queue, string jobId)
        {
            _transaction.QueueCommand(x => x.AddItemToSet(
                RedisStorage.Prefix + "queues", queue));

            _transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format(RedisStorage.Prefix + "queue:{0}", queue), jobId));
        }
    }
}