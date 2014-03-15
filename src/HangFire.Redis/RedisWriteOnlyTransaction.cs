using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis
{
    internal class RedisWriteOnlyTransaction : IWriteOnlyTransaction
    {
        private readonly IRedisTransaction _transaction;

        public RedisWriteOnlyTransaction(IRedisTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }

        public bool Commit()
        {
            return _transaction.Commit();
        }

        public void ExpireJob(string jobId, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format(RedisStorage.Prefix + "job:{0}", jobId),
                expireIn));

            _transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format(RedisStorage.Prefix + "job:{0}:history", jobId),
                expireIn));

            _transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format(RedisStorage.Prefix + "job:{0}:state", jobId),
                expireIn));
        }

        public void PersistJob(string jobId)
        {
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(RedisStorage.Prefix + "job:{0}", jobId)));
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(RedisStorage.Prefix + "job:{0}:history", jobId)));
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(RedisStorage.Prefix + "job:{0}:state", jobId)));
        }

        public void SetJobState(
            string jobId, string state, IDictionary<string, string> stateProperties)
        {
            _transaction.QueueCommand(x => x.SetEntryInHash(
                String.Format(RedisStorage.Prefix + "job:{0}", jobId),
                "State",
                state));

            _transaction.QueueCommand(x => x.RemoveEntry(
                String.Format(RedisStorage.Prefix + "job:{0}:state", jobId)));

            _transaction.QueueCommand(x => x.SetRangeInHash(
                String.Format(RedisStorage.Prefix + "job:{0}:state", jobId),
                stateProperties));
        }

        public void AppendJobHistory(
            string jobId, IDictionary<string, string> properties)
        {
            _transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format(RedisStorage.Prefix + "job:{0}:history", jobId),
                JobHelper.ToJson(properties)));
        }

        public void AddToQueue(string queue, string jobId)
        {
            _transaction.QueueCommand(x => x.AddItemToSet(
                RedisStorage.Prefix + "queues", queue));

            _transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format(RedisStorage.Prefix + "queue:{0}", queue), jobId));
        }

        public void IncrementCounter(string key)
        {
            _transaction.QueueCommand(x => x.IncrementValue(RedisStorage.Prefix + key));
        }

        public void IncrementCounter(string key, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.IncrementValue(RedisStorage.Prefix + key));
            _transaction.QueueCommand(x => x.ExpireEntryIn(RedisStorage.Prefix + key, expireIn));
        }

        public void DecrementCounter(string key)
        {
            _transaction.QueueCommand(x => x.DecrementValue(RedisStorage.Prefix + key));
        }

        public void DecrementCounter(string key, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.DecrementValue(RedisStorage.Prefix + key));
            _transaction.QueueCommand(x => x.ExpireEntryIn(RedisStorage.Prefix + key, expireIn));
        }

        public void AddToSet(string key, string value)
        {
            _transaction.QueueCommand(x => x.AddItemToSortedSet(
                RedisStorage.Prefix + key, value));
        }

        public void AddToSet(string key, string value, double score)
        {
            _transaction.QueueCommand(x => x.AddItemToSortedSet(
                RedisStorage.Prefix + key, value, score));
        }

        public void RemoveFromSet(string key, string value)
        {
            _transaction.QueueCommand(x => x.RemoveItemFromSortedSet(
                RedisStorage.Prefix + key, value));
        }

        public void InsertToList(string key, string value)
        {
            _transaction.QueueCommand(x => x.EnqueueItemOnList(
                RedisStorage.Prefix + key, value));
        }

        public void RemoveFromList(string key, string value)
        {
            _transaction.QueueCommand(x => x.RemoveItemFromList(
                RedisStorage.Prefix + key, value));
        }

        public void TrimList(
            string key, int keepStartingFrom, int keepEndingAt)
        {
            _transaction.QueueCommand(x => x.TrimList(
                RedisStorage.Prefix + key, keepStartingFrom, keepEndingAt));
        }

        public void IncrementValue(string key)
        {
            _transaction.QueueCommand(x => x.IncrementValue(
                RedisStorage.Prefix + key));
        }

        public void DecrementValue(string key)
        {
            _transaction.QueueCommand(x => x.DecrementValue(RedisStorage.Prefix + key));
        }

        public void ExpireValue(string key, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.ExpireEntryIn(
                RedisStorage.Prefix + key, expireIn));
        }
    }
}
