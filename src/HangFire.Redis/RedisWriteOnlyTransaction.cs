// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using ServiceStack.Redis;

namespace Hangfire.Redis
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

        public void Commit()
        {
            if (!_transaction.Commit())
            {
                // RedisTransaction.Commit returns false only when
                // WATCH condition has been failed. So, we should 
                // re-play the transaction.

                int replayCount = 1;
                const int maxReplayCount = 3;

                while (!_transaction.Replay())
                {
                    if (replayCount++ >= maxReplayCount)
                    {
                        throw new RedisException("Transaction commit was failed due to WATCH condition failure. Retry attempts exceeded.");
                    }
                }
            }
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

        public void SetJobState(string jobId, IState state)
        {
            _transaction.QueueCommand(x => x.SetEntryInHash(
                String.Format(RedisStorage.Prefix + "job:{0}", jobId),
                "State",
                state.Name));

            _transaction.QueueCommand(x => x.RemoveEntry(
                String.Format(RedisStorage.Prefix + "job:{0}:state", jobId)));

            var storedData = new Dictionary<string, string>(state.SerializeData());
            storedData.Add("State", state.Name);

            if (state.Reason != null)
            {
                storedData.Add("Reason", state.Reason);
            }

            _transaction.QueueCommand(x => x.SetRangeInHash(
                String.Format(RedisStorage.Prefix + "job:{0}:state", jobId),
                storedData));

            AddJobState(jobId, state);
        }

        public void AddJobState(string jobId, IState state)
        {
            var storedData = new Dictionary<string, string>(state.SerializeData());
            storedData.Add("State", state.Name);
            storedData.Add("Reason", state.Reason);
            storedData.Add("CreatedAt", JobHelper.SerializeDateTime(DateTime.UtcNow));

            _transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format(RedisStorage.Prefix + "job:{0}:history", jobId),
                JobHelper.ToJson(storedData)));
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

        public void SetRangeInHash(
            string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");

            _transaction.QueueCommand(
                x => x.SetRangeInHash(RedisStorage.GetRedisKey(key), keyValuePairs));
        }

        public void RemoveHash(string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            _transaction.QueueCommand(x => x.Remove(RedisStorage.GetRedisKey(key)));
        }
    }
}
