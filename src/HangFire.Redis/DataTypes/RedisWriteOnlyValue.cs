using System;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis.DataTypes
{
    internal class RedisWriteOnlyValue : IWriteOnlyPersistentValue
    {
        private readonly IRedisTransaction _transaction;

        public RedisWriteOnlyValue(IRedisTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        void IWriteOnlyPersistentValue.Increment(string key)
        {
            _transaction.QueueCommand(x => x.IncrementValue(
                RedisStorage.Prefix+ key));
        }

        void IWriteOnlyPersistentValue.Decrement(string key)
        {
            _transaction.QueueCommand(x => x.DecrementValue(RedisStorage.Prefix + key));
        }

        void IWriteOnlyPersistentValue.ExpireIn(string key, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.ExpireEntryIn(
                RedisStorage.Prefix + key, expireIn));
        }
    }
}