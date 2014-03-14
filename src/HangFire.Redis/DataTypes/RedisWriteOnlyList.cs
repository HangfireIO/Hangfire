using System;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis.DataTypes
{
    internal class RedisWriteOnlyList : IWriteOnlyPersistentList
    {
        private readonly IRedisTransaction _transaction;

        public RedisWriteOnlyList(IRedisTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void AddToLeft(string key, string value)
        {
            _transaction.QueueCommand(x => x.EnqueueItemOnList(
                RedisStorage.Prefix + key, value));
        }

        public void Remove(string key, string value)
        {
            _transaction.QueueCommand(x => x.RemoveItemFromList(
                RedisStorage.Prefix + key, value));
        }

        public void Trim(
            string key, int keepStartingFrom, int keepEndingAt)
        {
            _transaction.QueueCommand(x => x.TrimList(
                RedisStorage.Prefix + key, keepStartingFrom, keepEndingAt));
        }
    }
}