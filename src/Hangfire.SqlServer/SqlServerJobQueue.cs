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

            if (_options.SlidingInvisibilityTimeout.HasValue)
            {
                return DequeueUsingSlidingInvisibilityTimeout(queues, cancellationToken);
            }

            return DequeueUsingTransaction(queues, cancellationToken);
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
                new { jobId = long.Parse(jobId), queue = queue }
#if !NETFULL
                , transaction
#endif
                , commandTimeout: _storage.CommandTimeout);
        }

        private SqlServerTimeoutJob DequeueUsingSlidingInvisibilityTimeout(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", nameof(queues));

            FetchedJob fetchedJob = null;

            var fetchJobSqlTemplate = $@"
set transaction isolation level read committed
update top (1) JQ
set FetchedAt = GETUTCDATE()
output INSERTED.Id, INSERTED.JobId, INSERTED.Queue
from [{_storage.SchemaName}].JobQueue JQ with (readpast, updlock, rowlock, forceseek)
where Queue in @queues and
(FetchedAt is null or FetchedAt < DATEADD(second, @timeout, GETUTCDATE()))";

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                _storage.UseConnection(null, connection =>
                {
                    fetchedJob = connection
                        .Query<FetchedJob>(
                            fetchJobSqlTemplate,
                            new { queues = queues, timeout = _options.SlidingInvisibilityTimeout.Value.Negate().TotalSeconds })
                        .SingleOrDefault();
                });

                if (fetchedJob != null)
                {
                    return new SqlServerTimeoutJob(
                        _storage,
                        fetchedJob.Id,
                        fetchedJob.JobId.ToString(CultureInfo.InvariantCulture),
                        fetchedJob.Queue);
                }

                WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle, NewItemInQueueEvent }, _options.QueuePollInterval);
                cancellationToken.ThrowIfCancellationRequested();
            } while (true);
        }

        private SqlServerTransactionJob DequeueUsingTransaction(string[] queues, CancellationToken cancellationToken)
        {
            FetchedJob fetchedJob = null;
            DbTransaction transaction = null;

            string fetchJobSqlTemplate =
                $@"delete top (1) JQ
output DELETED.Id, DELETED.JobId, DELETED.Queue
from [{_storage.SchemaName}].JobQueue JQ with (readpast, updlock, rowlock, forceseek)
where Queue in @queues and (FetchedAt is null or FetchedAt < DATEADD(second, @timeout, GETUTCDATE()))";

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var connection = _storage.CreateAndOpenConnection();

                try
                {
                    transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                    fetchedJob = connection.Query<FetchedJob>(
                        fetchJobSqlTemplate,
#pragma warning disable 618
                        new { queues = queues, timeout = _options.InvisibilityTimeout.Negate().TotalSeconds },
#pragma warning restore 618
                        transaction,
                        commandTimeout: _storage.CommandTimeout).SingleOrDefault();

                    if (fetchedJob != null)
                    {
                        return new SqlServerTransactionJob(
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

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        private class FetchedJob
        {
            public long Id { get; set; }
            public long JobId { get; set; }
            public string Queue { get; set; }
        }
    }
}