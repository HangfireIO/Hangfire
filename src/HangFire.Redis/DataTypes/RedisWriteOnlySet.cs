using System;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis.DataTypes
{
    internal class RedisWriteOnlySet : IWriteOnlyPersistentSet
    {
        private readonly IRedisTransaction _transaction;

        public RedisWriteOnlySet(IRedisTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void Add(string key, string value)
        {
            _transaction.QueueCommand(x => x.AddItemToSortedSet(
                RedisStorage.Prefix + key, value));
        }

        public void Add(string key, string value, double score)
        {
            _transaction.QueueCommand(x => x.AddItemToSortedSet(
                RedisStorage.Prefix + key, value, score));
        }

        public void Remove(string key, string value)
        {
            _transaction.QueueCommand(x => x.RemoveItemFromSortedSet(
                RedisStorage.Prefix + key, value));
        }
    }
}