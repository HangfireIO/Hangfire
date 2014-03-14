using System;
using HangFire.Redis.DataTypes;
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

            Values = new RedisWriteOnlyValue(transaction);
            Sets = new RedisWriteOnlySet(transaction);
            Lists = new RedisWriteOnlyList(transaction);
            Queues = new RedisWriteOnlyQueue(transaction);
            Jobs = new RedisWriteOnlyJob(transaction);
            Counters = new RedisWriteOnlyCounter(transaction);
        }

        public IWriteOnlyPersistentValue Values { get; private set; }
        public IWriteOnlyPersistentSet Sets { get; private set; }
        public IWriteOnlyPersistentList Lists { get; private set; }
        public IWriteOnlyPersistentQueue Queues { get; private set; }
        public IWriteOnlyPersistentJob Jobs { get; private set; }
        public IWriteOnlyPersistentCounter Counters { get; private set; }

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
