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
using System.Data.Common;
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
        // This is an optimization that helps to overcome the polling delay, when
        // both client and server reside in the same process. Everything is working
        // without this event, but it helps to reduce the delays in processing.
        internal static readonly AutoResetEvent NewItemInQueueEvent = new AutoResetEvent(true);

        private readonly SqlServerStorage _storage;
        private readonly SqlServerStorageOptions _options;
		
        public SqlServerJobQueue([NotNull] SqlServerStorage storage, SqlServerStorageOptions options)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _storage = storage;
            _options = options;
        }

        [NotNull]
        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", nameof(queues));

            FetchedJob fetchedJob = null;
            DbConnection connection = null;
            DbTransaction transaction = null;

            string fetchJobSqlTemplate =
$@"delete top (1) from [{_storage.SchemaName}].JobQueue with (readpast, updlock, rowlock)
output DELETED.Id, DELETED.JobId, DELETED.Queue
where (FetchedAt is null or FetchedAt < DATEADD(second, @timeout, GETUTCDATE()))
and Queue in @queues";

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                connection = _storage.CreateAndOpenConnection();
                transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                try
                {
                    fetchedJob = connection.Query<FetchedJob>(
                               fetchJobSqlTemplate,
#pragma warning disable 618
                               new { queues = queues, timeout = _options.InvisibilityTimeout.Negate().TotalSeconds },
#pragma warning restore 618
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

                    WaitHandle.WaitAny(new []{ cancellationToken.WaitHandle, NewItemInQueueEvent },_options.QueuePollInterval);
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
            string enqueueJobSql =
$@"insert into [{_storage.SchemaName}].JobQueue (JobId, Queue) values (@jobId, @queue)";

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