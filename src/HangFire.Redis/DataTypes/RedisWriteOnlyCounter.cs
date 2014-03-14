using System;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis.DataTypes
{
    internal class RedisWriteOnlyCounter : IWriteOnlyPersistentCounter
    {
        private readonly IRedisTransaction _transaction;

        public RedisWriteOnlyCounter(IRedisTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void Increment(string key)
        {
            _transaction.QueueCommand(x => x.IncrementValue(RedisStorage.Prefix + key));
        }

        public void Increment(string key, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.IncrementValue(RedisStorage.Prefix + key));
            _transaction.QueueCommand(x => x.ExpireEntryIn(RedisStorage.Prefix + key, expireIn));
        }

        public void Decrement(string key)
        {
            _transaction.QueueCommand(x => x.DecrementValue(RedisStorage.Prefix + key));
        }

        public void Decrement(string key, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.DecrementValue(RedisStorage.Prefix + key));
            _transaction.QueueCommand(x => x.ExpireEntryIn(RedisStorage.Prefix + key, expireIn));
        }
    }
}