using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis
{
    internal class RedisStoredValues : 
        IWriteableStoredSets, IWriteableStoredValues, IWriteableJobQueue,
        IWriteableStoredJobs, IWriteableStoredLists
    {
        private const string Prefix = "hangfire:";
        private readonly IRedisTransaction _transaction;

        public RedisStoredValues(IRedisTransaction transaction)
        {
            _transaction = transaction;
        }

        void IWriteableStoredSets.Add(string key, string value)
        {
            _transaction.QueueCommand(x => x.AddItemToSortedSet(
                Prefix + key, value));
        }

        void IWriteableStoredSets.Add(string key, string value, double score)
        {
            _transaction.QueueCommand(x => x.AddItemToSortedSet(
                Prefix + key, value, score));
        }

        void IWriteableStoredSets.Remove(string key, string value)
        {
            _transaction.QueueCommand(x => x.RemoveItemFromSortedSet(
                Prefix + key, value));
        }

        void IWriteableStoredValues.Increment(string key)
        {
            _transaction.QueueCommand(x => x.IncrementValue(
                Prefix + key));
        }

        void IWriteableStoredValues.Decrement(string key)
        {
            _transaction.QueueCommand(x => x.DecrementValue(Prefix + key));
        }

        void IWriteableStoredValues.ExpireIn(string key, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.ExpireEntryIn(
                Prefix + key, expireIn));
        }

        void IWriteableJobQueue.Enqueue(string queue, string jobId)
        {
            _transaction.QueueCommand(x => x.AddItemToSet(Prefix + "queues", queue));
            _transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format(Prefix + "queue:{0}", queue), jobId));
        }

        void IWriteableStoredJobs.Expire(string jobId, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format(Prefix + "job:{0}", jobId),
                expireIn));

            _transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format(Prefix + "job:{0}:history", jobId),
                expireIn));

            _transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format(Prefix + "job:{0}:state", jobId),
                expireIn));
        }

        void IWriteableStoredJobs.Create(string jobId, IDictionary<string, string> parameters)
        {
            _transaction.QueueCommand(x => x.SetRangeInHash(
                String.Format(Prefix + "job:{0}", jobId),
                parameters));
        }

        void IWriteableStoredJobs.Persist(string jobId)
        {
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(Prefix + "job:{0}", jobId)));
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(Prefix + "job:{0}:history", jobId)));
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(Prefix + "job:{0}:state", jobId)));
        }

        void IWriteableStoredJobs.SetState(
            string jobId, string state, Dictionary<string, string> stateProperties)
        {
            _transaction.QueueCommand(x => x.SetEntryInHash(
                String.Format(Prefix + "job:{0}", jobId),
                "State",
                state));

            _transaction.QueueCommand(x => x.RemoveEntry(
                String.Format(Prefix + "job:{0}:state", jobId)));

            _transaction.QueueCommand(x => x.SetRangeInHash(
                String.Format(Prefix + "job:{0}:state", jobId),
                stateProperties));
        }

        void IWriteableStoredJobs.AppendHistory(
            string jobId, Dictionary<string, string> properties)
        {
            _transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format(Prefix + "job:{0}:history", jobId),
                JobHelper.ToJson(properties)));
        }

        void IWriteableStoredLists.AddToLeft(string key, string value)
        {
            _transaction.QueueCommand(x => x.EnqueueItemOnList(Prefix + key, value));
        }

        void IWriteableStoredLists.Remove(string key, string value)
        {
            _transaction.QueueCommand(x => x.RemoveItemFromList(
                Prefix + key, value));
        }

        void IWriteableStoredLists.Trim(
            string key, int keepStartingFrom, int keepEndingAt)
        {
            _transaction.QueueCommand(x => x.TrimList(
                Prefix + key, keepStartingFrom, keepEndingAt));   
        }
    }
}