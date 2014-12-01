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
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using Dapper;
using Hangfire.Sql.Properties;
using Hangfire.Storage;

namespace Hangfire.Sql {
    public class SqlJobQueue : IPersistentJobQueue {
        protected SqlStorageOptions Options { get; private set; }
        protected IConnectionProvider ConnectionProvider { get; private set; }
        protected SqlBook SqlBook { get; private set; }

        public SqlJobQueue(IConnectionProvider connectionProvider, SqlBook sqlBook, SqlStorageOptions options) {
            if (options == null) throw new ArgumentNullException("options");
            if (connectionProvider == null) throw new ArgumentNullException("connectionProvider");

            Options = options;
            ConnectionProvider = connectionProvider;
            SqlBook = sqlBook;
        }

        [NotNull]
        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken) {
            if (queues == null) throw new ArgumentNullException("queues");
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");
            FetchedJob fetchedJob;
            using (var connection = ConnectionProvider.CreateAndOpenConnection()) {
                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted)) {
                    do {
                        cancellationToken.ThrowIfCancellationRequested();
                        // Sql query is splitted to force SQL Server to use 
                        // INDEX SEEK instead of INDEX SCAN operator.
                        fetchedJob = QueryNeverFetchedJob(queues);
                        if (fetchedJob != null) {
                            break;
                        }
                        fetchedJob = QueryFetchedAtJob(queues);
                        if (fetchedJob == null) {
                            cancellationToken.WaitHandle.WaitOne(Options.QueuePollInterval);
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    } while (fetchedJob == null);
                    transaction.Commit();
                }
            }
            return new SqlFetchedJob(
                ConnectionProvider,
                SqlBook,
                fetchedJob.Id,
                fetchedJob.JobId.ToString(CultureInfo.InvariantCulture),
                fetchedJob.Queue);
        }

        protected virtual FetchedJob QueryNeverFetchedJob(string[] queues) {
            return QueryFetchedJob(queues, SqlBook.SqlJobQueue_Dequeue_fetched_null);
        }

        protected virtual FetchedJob QueryFetchedAtJob(string[] queues) {
            return QueryFetchedJob(queues, SqlBook.SqlJobQueue_Dequeue_fetched_before);
        }

        private FetchedJob QueryFetchedJob(string[] queues, string sql) {
            using (var connection = ConnectionProvider.CreateAndOpenConnection()) {
                return connection.Query<FetchedJob>(
                    sql,
                    new {
                        queues = queues,
                        timeout = Options.InvisibilityTimeout.Negate().TotalSeconds
                    })
                    .SingleOrDefault();
            }
        }

        public void Enqueue(string queue, string jobId) {
            using (var connection = ConnectionProvider.CreateAndOpenConnection()) {
                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted)) {
                    connection.Execute(SqlBook.SqlJobQueue_Enqueue,
                        new { jobId = jobId, queue = queue },
                        transaction);
                    transaction.Commit();
                }
            }
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class FetchedJob {
            public int Id { get; set; }
            public int JobId { get; set; }
            public string Queue { get; set; }
        }
    }
}