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
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using Dapper;
using Hangfire.Annotations;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    internal class SqlServerJobQueue : IPersistentJobQueue
    {
        private readonly SqlServerStorage _storage;
        private readonly SqlServerStorageOptions _options;

        public SqlServerJobQueue([NotNull] SqlServerStorage storage, SqlServerStorageOptions options)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (options == null) throw new ArgumentNullException("options");

            _storage = storage;
            _options = options;
        }

        [NotNull]
        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");

            FetchedJob fetchedJob = null;
            IDbConnection connection = null;
            IDbTransaction transaction = null;

            string fetchJobSqlTemplate = string.Format(@"
delete top (1) from [{0}].JobQueue with (readpast, updlock, rowlock)
output DELETED.Id, DELETED.JobId, DELETED.Queue
where (FetchedAt is null or FetchedAt < DATEADD(second, @timeout, GETUTCDATE()))
and Queue in @queues", _storage.GetSchemaName());

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                connection = _storage.CreateAndOpenConnection();
                transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                try
                {
                    fetchedJob = connection.Query<FetchedJob>(
                               fetchJobSqlTemplate,
                               new { queues = queues, timeout = _options.InvisibilityTimeout.Negate().TotalSeconds },
                               transaction)
                               .SingleOrDefault();
                }
                catch (SqlException)
                {
                    transaction.Dispose();
                    _storage.ReleaseConnection(connection);
                    throw;
                }

                if (fetchedJob == null)
                {
                    transaction.Rollback();
                    transaction.Dispose();
                    _storage.ReleaseConnection(connection);

                    cancellationToken.WaitHandle.WaitOne(_options.QueuePollInterval);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            } while (fetchedJob == null);

            return new SqlServerFetchedJob(
                _storage,
                connection,
                transaction,
                fetchedJob.JobId.ToString(CultureInfo.InvariantCulture),
                fetchedJob.Queue);
        }

        public void Enqueue(IDbConnection connection, string queue, string jobId)
        {
            string enqueueJobSql = string.Format(@"
insert into [{0}].JobQueue (JobId, Queue) values (@jobId, @queue)", _storage.GetSchemaName());

            connection.Execute(enqueueJobSql, new { jobId = jobId, queue = queue });
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        private class FetchedJob
        {
            public int Id { get; set; }
            public int JobId { get; set; }
            public string Queue { get; set; }
        }
    }
}