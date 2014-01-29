using System;
using ServiceStack.Redis;

namespace HangFire.Storage
{
    internal class RedisAtomicWriteTransaction : IAtomicWriteTransaction
    {
        private readonly IRedisTransaction _transaction;

        public RedisAtomicWriteTransaction(IRedisTransaction transaction)
        {
            _transaction = transaction;

            var storage = new RedisStoredValues(_transaction);

            Values = storage;
            Sets = storage;
            Lists = storage;
            Queues = storage;
            Jobs = storage;
        }

        public IWriteableStoredValues Values { get; private set; }
        public IWriteableStoredSets Sets { get; private set; }
        public IWriteableStoredLists Lists { get; private set; }
        public IWriteableJobQueue Queues { get; private set; }
        public IWriteableStoredJobs Jobs { get; private set; }

        public bool Commit()
        {
            return _transaction.Commit();
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }
    }

    public interface IAtomicWriteTransaction : IDisposable
    {
        IWriteableStoredValues Values { get; }
        IWriteableStoredSets Sets { get; }
        IWriteableStoredLists Lists { get; }
        IWriteableJobQueue Queues { get; }
        IWriteableStoredJobs Jobs { get; }

        bool Commit();
    }

    public interface IWriteableStoredValues
    {
        void Increment(string key);
        void Decrement(string key);

        void ExpireIn(string key, TimeSpan expireIn);
    }

    public interface IWriteableStoredSets
    {
        void Add(string key, string value);
        void Add(string key, string value, double score);
        void Remove(string key, string value);
    }

    public interface IWriteableJobQueue
    {
        void Enqueue(string queue, string jobId);
    }

    public interface IWriteableStoredJobs
    {
        void Expire(string jobId, TimeSpan expireIn);
        void Persist(string jobId);
    }

    public interface IWriteableStoredLists
    {
        void AddToLeft(string key, string value);
        void Remove(string key, string value);

        void Trim(string key, int keepStartingFrom, int keepEndingAt);
    }

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

        void IWriteableStoredJobs.Persist(string jobId)
        {
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                    String.Format(Prefix + "job:{0}", jobId)));
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(Prefix + "job:{0}:history", jobId)));
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(Prefix + "job:{0}:state", jobId)));
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
