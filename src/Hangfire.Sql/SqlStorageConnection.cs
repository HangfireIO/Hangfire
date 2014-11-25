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
using System.Linq;
using System.Threading;
using System.Transactions;
using Dapper;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Sql.Entities;
using Hangfire.Storage;

namespace Hangfire.Sql {
    public class SqlStorageConnection : IStorageConnection {
        private readonly IDistributedLockAcquirer _distributedLockAcquirer;
        private readonly PersistentJobQueueProviderCollection _queueProviders;
        protected SqlBook SqlBook { get; private set; }
        protected IDbConnection Connection { get; private set; }

        public SqlStorageConnection(
            IDbConnection connection,
            SqlBook sqlBook,
            IDistributedLockAcquirer distributedLockAcquirer,
            PersistentJobQueueProviderCollection queueProviders) {
            if (connection == null) throw new ArgumentNullException("connection");
            if (queueProviders == null) throw new ArgumentNullException("queueProviders");

            Connection = connection;
            SqlBook = sqlBook;
            _distributedLockAcquirer = distributedLockAcquirer;
            _queueProviders = queueProviders;
        }

        public void Dispose() {
            Connection.Dispose();
        }

        public IWriteOnlyTransaction CreateWriteTransaction() {
            return new SqlWriteOnlyTransaction(Connection, SqlBook, _queueProviders);
        }

        public IDistributedLock AcquireDistributedLock(string resource, TimeSpan timeout) {
            return _distributedLockAcquirer.AcquireLock(resource, timeout, Connection);
        }

        public IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken) {
            if (queues == null || queues.Length == 0) throw new ArgumentNullException("queues");

            var providers = queues
                .Select(queue => _queueProviders.GetProvider(queue))
                .Distinct()
                .ToArray();

            if (providers.Length != 1) {
                throw new InvalidOperationException(String.Format(
                    "Multiple provider instances registered for queues: {0}. You should choose only one type of persistent queues per server instance.",
                    String.Join(", ", queues)));
            }

            var persistentQueue = providers[0].GetJobQueue(Connection);
            return persistentQueue.Dequeue(queues, cancellationToken);
        }

        public string CreateExpiredJob(
            Job job,
            IDictionary<string, string> parameters,
            DateTime createdAt,
            TimeSpan expireIn) {
            if (job == null) throw new ArgumentNullException("job");
            if (parameters == null) throw new ArgumentNullException("parameters");

            var invocationData = InvocationData.Serialize(job);

            var jobId = Connection.Query<int>(
                SqlBook.SqlConnection_CreateExpiredJob_Job,
                new {
                    invocationData = JobHelper.ToJson(invocationData),
                    arguments = invocationData.Arguments,
                    createdAt = createdAt,
                    expireAt = createdAt.Add(expireIn)
                }).Single().ToString();

            if (parameters.Count > 0) {
                var parameterArray = new object[parameters.Count];
                int parameterIndex = 0;
                foreach (var parameter in parameters) {
                    parameterArray[parameterIndex++] = new {
                        jobId = jobId,
                        name = parameter.Key,
                        value = parameter.Value
                    };
                }
                Connection.Execute(SqlBook.SqlConnection_CreateExpiredJob_Parameter, parameterArray);
            }

            return jobId;
        }

        public JobData GetJobData(string id) {
            if (id == null) throw new ArgumentNullException("id");

            var jobData = Connection.Query<SqlJob>(SqlBook.SqlConnection_GetJobData, new {id = id})
                .SingleOrDefault();

            if (jobData == null) return null;

            // TODO: conversion exception could be thrown.
            var invocationData = JobHelper.FromJson<InvocationData>(jobData.InvocationData);
            invocationData.Arguments = jobData.Arguments;

            Job job = null;
            JobLoadException loadException = null;

            try {
                job = invocationData.Deserialize();
            }
            catch (JobLoadException ex) {
                loadException = ex;
            }

            return new JobData {
                Job = job,
                State = jobData.StateName,
                CreatedAt = jobData.CreatedAt,
                LoadException = loadException
            };
        }

        public StateData GetStateData(string jobId) {
            if (jobId == null) throw new ArgumentNullException("jobId");

            var sqlState = Connection.Query<SqlState>(SqlBook.SqlConnection_GetStateData, new {jobId = jobId}).SingleOrDefault();
            if (sqlState == null) {
                return null;
            }

            return new StateData {
                Name = sqlState.Name,
                Reason = sqlState.Reason,
                Data = JobHelper.FromJson<Dictionary<string, string>>(sqlState.Data)
            };
        }

        public void SetJobParameter(string id, string name, string value) {
            if (id == null) throw new ArgumentNullException("id");
            if (name == null) throw new ArgumentNullException("name");
            Connection.Execute(
                SqlBook.SqlConnection_SetJobParameter,
                new {jobId = id, name, value});
        }

        public string GetJobParameter(string id, string name) {
            if (id == null) throw new ArgumentNullException("id");
            if (name == null) throw new ArgumentNullException("name");

            return Connection.Query<string>(
                SqlBook.SqlConnection_GetJobParameter,
                new {id = id, name = name})
                .SingleOrDefault();
        }

        public HashSet<string> GetAllItemsFromSet(string key) {
            if (key == null) throw new ArgumentNullException("key");

            var result = Connection.Query<string>(
                SqlBook.SqlConnection_GetAllItemsFromSet,
                new {key});

            return new HashSet<string>(result);
        }

        public string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore) {
            if (key == null) throw new ArgumentNullException("key");
            if (toScore < fromScore)
                throw new ArgumentException("The `toScore` value must be higher or equal to the `fromScore` value.");

            return Connection.Query<string>(
                SqlBook.SqlConnection_GetFirstByLowestScoreFromSet,
                new {key, from = fromScore, to = toScore})
                .SingleOrDefault();
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs) {
            if (key == null) throw new ArgumentNullException("key");
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");

            using (var transaction = new TransactionScope()) {
                foreach (var keyValuePair in keyValuePairs) {
                    Connection.Execute(SqlBook.SqlConnection_GetFirstByLowestScoreFromSet, 
                        new { key = key, field = keyValuePair.Key, value = keyValuePair.Value });
                }

                transaction.Complete();
            }
        }

        public Dictionary<string, string> GetAllEntriesFromHash(string key) {
            if (key == null) throw new ArgumentNullException("key");

            var result = Connection.Query<SqlHash>(
                SqlBook.SqlConnection_GetAllEntriesFromHash,
                new {key})
                .ToDictionary(x => x.Field, x => x.Value);

            return result.Count != 0 ? result : null;
        }

        public virtual void AnnounceServer(string serverId, ServerContext context) {
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (context == null) throw new ArgumentNullException("context");

            var data = new ServerData {
                WorkerCount = context.WorkerCount,
                Queues = context.Queues,
                StartedAt = DateTime.UtcNow,
            };

            Connection.Execute(SqlBook.SqlConnection_AnnounceServer,
                new {id = serverId, data = JobHelper.ToJson(data), heartbeat = DateTime.UtcNow});
        }

        public void RemoveServer(string serverId) {
            if (serverId == null) throw new ArgumentNullException("serverId");

            Connection.Execute(
                SqlBook.SqlConnection_RemoveServer,
                new {id = serverId});
        }

        public void Heartbeat(string serverId) {
            if (serverId == null) throw new ArgumentNullException("serverId");

            Connection.Execute(
                SqlBook.SqlConnection_Heartbeat,
                new {now = DateTime.UtcNow, id = serverId});
        }

        public int RemoveTimedOutServers(TimeSpan timeOut) {
            if (timeOut.Duration() != timeOut) {
                throw new ArgumentException("The `timeOut` value must be positive.", "timeOut");
            }

            return Connection.Execute(
                SqlBook.SqlConnection_RemoveTimedOutServers,
                new {timeOutAt = DateTime.UtcNow.Add(timeOut.Negate())});
        }
    }
}