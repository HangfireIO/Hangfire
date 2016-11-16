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
using System.Globalization;
using System.Linq;
using System.Threading;
using Dapper;
using Hangfire.Annotations;
using Hangfire.Storage;

// ReSharper disable RedundantAnonymousTypePropertyName

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
            DbTransaction transaction = null;

            string fetchJobSqlTemplate =
$@"delete top (1) JQ
output DELETED.Id, DELETED.JobId, DELETED.Queue
from [{_storage.SchemaName}].JobQueue JQ with (readpast, updlock, rowlock, forceseek)
where Queue in @queues";

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var connection = _storage.CreateAndOpenConnection();

                try
                {
                    transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                    fetchedJob = connection.Query<FetchedJob>(
                        fetchJobSqlTemplate,
                        new { queues = queues },
                        transaction).SingleOrDefault();

                    if (fetchedJob != null)
                    {
                        return new SqlServerFetchedJob(
                            _storage,
                            connection,
                            transaction,
                            fetchedJob.JobId.ToString(CultureInfo.InvariantCulture),
                            fetchedJob.Queue);
                    }
                }
                finally
                {
                    if (fetchedJob == null)
                    {
                        transaction?.Dispose();
                        transaction = null;

                        _storage.ReleaseConnection(connection);
                    }
                }

                WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle, NewItemInQueueEvent }, _options.QueuePollInterval);
                cancellationToken.ThrowIfCancellationRequested();
            } while (true);
        }

#if NETFULL
        public void Enqueue(IDbConnection connection, string queue, string jobId)
#else
        public void Enqueue(DbConnection connection, DbTransaction transaction, string queue, string jobId)
#endif
        {
            string enqueueJobSql =
$@"insert into [{_storage.SchemaName}].JobQueue (JobId, Queue) values (@jobId, @queue)";

            connection.Execute(
                enqueueJobSql, 
                new { jobId = int.Parse(jobId), queue = queue }
#if !NETFULL
                , transaction
#endif
                );
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