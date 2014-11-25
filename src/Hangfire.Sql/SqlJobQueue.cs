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

namespace Hangfire.Sql
{
    public class SqlJobQueue : IPersistentJobQueue
    {
        protected SqlStorageOptions Options { get; private set; }
        protected IDbConnection Connection { get; private set; }
        protected SqlBook SqlBook { get; private set; }

        public SqlJobQueue(IDbConnection connection, SqlBook sqlBook, SqlStorageOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (connection == null) throw new ArgumentNullException("connection");

            Options = options;
            Connection = connection;
            SqlBook = sqlBook;
        }

        [NotNull]
        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");
            FetchedJob fetchedJob;
            using (var transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted)) {
                do {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Sql query is splitted to force SQL Server to use 
                    // INDEX SEEK instead of INDEX SCAN operator.
                    fetchedJob = QueryFetchedJob(queues, SqlBook.SqlJobQueue_Dequeue_fetched_null);
                    if (fetchedJob != null) {
                        break;
                    }
                    fetchedJob = QueryFetchedJob(queues, SqlBook.SqlJobQueue_Dequeue_fetched_before);
                    if (fetchedJob == null) {
                        cancellationToken.WaitHandle.WaitOne(Options.QueuePollInterval);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                } while (fetchedJob == null);
                transaction.Commit();
            }
            return new SqlFetchedJob(
                Connection,
                SqlBook,
                fetchedJob.Id,
                fetchedJob.JobId.ToString(CultureInfo.InvariantCulture),
                fetchedJob.Queue);
        }

        protected virtual FetchedJob QueryFetchedJob(string[] queues, string sql) {
            return Connection.Query<FetchedJob>(
                sql,
                new {
                    queues = queues, 
                    timeout = Options.InvisibilityTimeout.Negate().TotalSeconds
                })
                .SingleOrDefault();
        }

        public void Enqueue(string queue, string jobId)
        {
            Connection.Execute(SqlBook.SqlJobQueue_Enqueue, new { jobId = jobId, queue = queue });
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class FetchedJob
        {
            public int Id { get; set; }
            public int JobId { get; set; }
            public string Queue { get; set; }
        }
    }
}