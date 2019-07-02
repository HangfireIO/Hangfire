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
using Hangfire.Common;
using Hangfire.Storage;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Hangfire.SqlServer
{
    internal class SqlServerJobQueue : IPersistentJobQueue
    {
        // This is an optimization that helps to overcome the polling delay, when
        // both client and server reside in the same process. Everything is working
        // without this event, but it helps to reduce the delays in processing.
        internal static readonly AutoResetEvent NewItemInQueueEvent = new AutoResetEvent(false);

        private static readonly TimeSpan LongPollingThreshold = TimeSpan.FromSeconds(1);
        private static readonly int PollingQuantumMs = 1000;
        private static readonly int MinPollingDelayMs = 50;

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

#if FEATURE_TRANSACTIONSCOPE
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
#if !FEATURE_TRANSACTIONSCOPE
                , transaction
#endif
                , commandTimeout: _storage.CommandTimeout);
        }

        private SqlServerTimeoutJob DequeueUsingSlidingInvisibilityTimeout(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", nameof(queues));

            var lockResource = $"{_storage.SchemaName}_FetchLockLock_{String.Join("_", queues.OrderBy(x => x))}";
            var isBlocking = false;

            var pollingDelayMs = Math.Min(
                Math.Max((int)_options.QueuePollInterval.TotalMilliseconds, MinPollingDelayMs),
                PollingQuantumMs);

            SqlServerTimeoutJob fetched;

            using (var cancellationEvent = cancellationToken.GetCancellationEvent())
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int? lockResult = null;

                    fetched = _storage.UseConnection(null, connection =>
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@queues", queues);
                        parameters.Add("@timeout", (int)_options.SlidingInvisibilityTimeout.Value.Negate().TotalSeconds);
                        parameters.Add("@lockResource", lockResource);
                        parameters.Add("@pollingDelayMs", pollingDelayMs);
                        parameters.Add("@pollingQuantumMs", PollingQuantumMs);
                        parameters.Add("@result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                        var query = isBlocking ? GetBlockingFetchSql() : GetNonBlockingFetchSql();

                        using (var reader = connection.QueryMultiple(query, parameters, commandTimeout: _storage.CommandTimeout))
                        {
                            while (!reader.IsConsumed)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                var fetchedJob = reader.Read<FetchedJob>().SingleOrDefault(x => x != null);
                                if (fetchedJob != null && !(fetchedJob.Id == 0 && fetchedJob.JobId == 0 && fetchedJob.Queue == null))
                                {
                                    return new SqlServerTimeoutJob(_storage, fetchedJob.Id, fetchedJob.JobId.ToString(CultureInfo.InvariantCulture), fetchedJob.Queue, fetchedJob.FetchedAt.Value);
                                }
                            }
                        }

                        lockResult = parameters.Get<int?>("@result");
                        return null;
                    });

                    if (fetched != null)
                    {
                        break;
                    }

                    if (lockResult.HasValue && lockResult.Value < -1)
                    {
                        throw new InvalidOperationException($"A call to sp_getapplock returned unexpected result '{lockResult.Value}' while fetching a job. Please report this problem to Hangfire developers and don't use sub-second values for the QueuePollInterval option.");
                    }

                    if (_options.QueuePollInterval < LongPollingThreshold)
                    {
                        isBlocking = true;
                    }
                    else
                    {
                        WaitHandle.WaitAny(new WaitHandle[] { cancellationEvent.WaitHandle, NewItemInQueueEvent }, _options.QueuePollInterval);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                } while (true);
            }

            return fetched;
        }

        private string GetNonBlockingFetchSql()
        {
            return $@"
set nocount on;
set xact_abort on;
set transaction isolation level read committed;

update top (1) JQ
set FetchedAt = GETUTCDATE()
output INSERTED.Id, INSERTED.JobId, INSERTED.Queue, INSERTED.FetchedAt
from [{_storage.SchemaName}].JobQueue JQ with ({GetSlidingFetchTableHints()})
where Queue in @queues and
(FetchedAt is null or FetchedAt < DATEADD(second, @timeout, GETUTCDATE()));";
        }

        private string GetBlockingFetchSql()
        {
            return $@"
set nocount on;
set xact_abort on;
set transaction isolation level read committed;

EXEC @result = sp_getapplock @Resource = @lockResource, @LockMode = 'Exclusive', @LockTimeout = @pollingQuantumMs, @LockOwner = 'Session';

IF (@result >= 0)
BEGIN
    declare @now DATETIME2 = SYSUTCDATETIME();
    declare @pollingDelay datetime = dateadd(ms, @pollingDelayMs, convert(DATETIME, 0));
    declare @quantumEnd datetime2 = DATEADD(ms, @pollingQuantumMs, @now);

    WHILE (@now < @quantumEnd)
    BEGIN
        update top (1) JQ
        set FetchedAt = @now
        output INSERTED.Id, INSERTED.JobId, INSERTED.Queue, INSERTED.FetchedAt
        from [{_storage.SchemaName}].JobQueue JQ with ({GetSlidingFetchTableHints()})
        where Queue in @queues and
        (FetchedAt is null or FetchedAt < DATEADD(second, @timeout, @now));

        IF @@ROWCOUNT > 0
        BEGIN
            EXEC sp_releaseapplock @Resource = @lockResource, @LockOwner = 'Session';
            RETURN;
        END;

        WAITFOR DELAY @pollingDelay;
        SET @now = SYSUTCDATETIME();
    END
    EXEC sp_releaseapplock @Resource = @lockResource, @LockOwner = 'Session';
END

SELECT 0 AS [Id], CAST(0 AS BIGINT) AS [JobId], CAST(NULL AS NVARCHAR) as [Queue], CAST(NULL AS DATETIME) as [FetchedAt];";
        }

        private string GetSlidingFetchTableHints()
        {
            if (_storage.Options.UsePageLocksOnDequeue)
            {
                return "forceseek, paglock, xlock";
            }

            return "forceseek, readpast, updlock, rowlock";
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

            var pollInterval = _options.QueuePollInterval > TimeSpan.Zero
                ? _options.QueuePollInterval
                : TimeSpan.FromSeconds(1);

            using (var cancellationEvent = cancellationToken.GetCancellationEvent())
            {
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

                    WaitHandle.WaitAny(new WaitHandle[] { cancellationEvent.WaitHandle, NewItemInQueueEvent }, pollInterval);
                    cancellationToken.ThrowIfCancellationRequested();
                } while (true);
            }
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        private class FetchedJob
        {
            public long Id { get; set; }
            public long JobId { get; set; }
            public string Queue { get; set; }
            public DateTime? FetchedAt { get; set; }
        }
    }
}