using ServiceStack.Redis;

namespace HangFire.Storage.Redis
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
}
