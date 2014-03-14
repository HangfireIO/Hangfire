using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis.DataTypes
{
    internal class RedisWriteOnlyJob : IWriteOnlyPersistentJob
    {
        private readonly IRedisTransaction _transaction;

        public RedisWriteOnlyJob(IRedisTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void Expire(string jobId, TimeSpan expireIn)
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

        public void Persist(string jobId)
        {
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(RedisStorage.Prefix + "job:{0}", jobId)));
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(RedisStorage.Prefix + "job:{0}:history", jobId)));
            _transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format(RedisStorage.Prefix + "job:{0}:state", jobId)));
        }

        public void SetState(
            string jobId, string state, Dictionary<string, string> stateProperties)
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

        public void AppendHistory(
            string jobId, Dictionary<string, string> properties)
        {
            _transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format(RedisStorage.Prefix + "job:{0}:history", jobId),
                JobHelper.ToJson(properties)));
        }
    }
}