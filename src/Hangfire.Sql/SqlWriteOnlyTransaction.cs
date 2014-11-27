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
using System.Data;
using Dapper;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using IsolationLevel = System.Data.IsolationLevel;

namespace Hangfire.Sql {
    public class SqlWriteOnlyTransaction : IWriteOnlyTransaction {
        private readonly Queue<Action<IDbConnection, IDbTransaction>> _commandQueue
            = new Queue<Action<IDbConnection, IDbTransaction>>();

        private readonly IDbConnection _connection;
        private readonly PersistentJobQueueProviderCollection _queueProviders;
        protected readonly SqlBook SqlBook;

        public SqlWriteOnlyTransaction(
            IDbConnection connection,
            SqlBook sqlBook,
            PersistentJobQueueProviderCollection queueProviders) {
            if (connection == null) throw new ArgumentNullException("connection");
            if (queueProviders == null) throw new ArgumentNullException("queueProviders");

            _connection = connection;
            SqlBook = sqlBook;
            _queueProviders = queueProviders;
        }

        public void Dispose() {
        }

        public void Commit() {
            using (var transaction = _connection.BeginTransaction(IsolationLevel.Serializable)) {
                foreach (Action<IDbConnection, IDbTransaction> command in _commandQueue) {
                    command(_connection, transaction);
                }
                transaction.Commit();
            }
        }

        public void ExpireJob(string jobId, TimeSpan expireIn) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_ExpireJob,
                new { expireAt = DateTime.UtcNow.Add(expireIn), id = jobId },
                t));
        }

        public void PersistJob(string jobId) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_PersistJob,
                new { pid = jobId },
                t));
        }

        public virtual void SetJobState(string jobId, IState state) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_SetJobState,
                new {
                    jobId = jobId,
                    name = state.Name,
                    reason = state.Reason,
                    createdAt = DateTime.UtcNow,
                    data = JobHelper.ToJson(state.SerializeData()),
                    id = jobId
                },t));
        }

        public void AddJobState(string jobId, IState state) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_AddJobState,
                new {
                    jobId = jobId,
                    name = state.Name,
                    reason = state.Reason,
                    createdAt = DateTime.UtcNow,
                    data = JobHelper.ToJson(state.SerializeData())
                },t));
        }

        public void AddToQueue(string queue, string jobId) {
            var provider = _queueProviders.GetProvider(queue);
            var persistentQueue = provider.GetJobQueue();
            QueueCommand((c,t) => persistentQueue.Enqueue(queue, jobId));
        }

        public void IncrementCounter(string key) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_IncrementCounter,
                new { key, value = +1 },t));
        }

        public void IncrementCounter(string key, TimeSpan expireIn) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_IncrementCounter_expirein,
                new { key, value = +1, expireAt = DateTime.UtcNow.Add(expireIn) },t));
        }

        public void DecrementCounter(string key) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_DecrementCounter,
                new { key, value = -1 },t));
        }

        public void DecrementCounter(string key, TimeSpan expireIn) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_DecrementCounter,
                new { key, value = -1, expireAt = DateTime.UtcNow.Add(expireIn) },t));
        }

        public void AddToSet(string key, string value) {
            AddToSet(key, value, 0.0);
        }

        public void AddToSet(string key, string value, double score) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_AddToSet,
                new { key, value, score },t));
        }

        public void RemoveFromSet(string key, string value) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_RemoveFromSet,
                new { key, value },t));
        }

        public void InsertToList(string key, string value) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_InsertToList,
                new { key, value },t));
        }

        public void RemoveFromList(string key, string value) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_RemoveFromList,
                new { key, value },t));
        }

        public void TrimList(string key, int keepStartingFrom, int keepEndingAt) {
            QueueCommand((c,t) => c.Execute(
                SqlBook.SqlWriteOnlyTransaction_TrimList,
                new { key = key, start = keepStartingFrom + 1, end = keepEndingAt + 1 },t));
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs) {
            if (key == null) throw new ArgumentNullException("key");
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");

            foreach (var keyValuePair in keyValuePairs) {
                var pair = keyValuePair;

                QueueCommand((c,t) => {
                        c.Execute(SqlBook.SqlWriteOnlyTransaction_SetRangeInHash,
                            new {key = key, field = pair.Key, value = pair.Value},t);
                    }
                );
            }
        }

        public void RemoveHash(string key) {
            if (key == null) throw new ArgumentNullException("key");

            QueueCommand((c,t) => c.Execute(SqlBook.SqlWriteOnlyTransaction_RemoveHash,
                new { key },t));
        }

        protected internal void QueueCommand(Action<IDbConnection, IDbTransaction> action) {
            _commandQueue.Enqueue(action);
        }
    }
}