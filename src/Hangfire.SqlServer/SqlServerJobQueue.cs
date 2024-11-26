// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Hangfire.SqlServer
{
    internal sealed class SqlServerJobQueue : IPersistentJobQueue
    {
        // This is an optimization that helps to overcome the polling delay, when
        // both client and server reside in the same process. Everything is working
        // without these events, but it helps to reduce the delays in processing.
        internal static readonly ConcurrentDictionary<Tuple<SqlServerStorage, string>, AutoResetEvent> NewItemInQueueEvents = new();

        private static readonly Func<Tuple<SqlServerStorage, string>, SemaphoreSlim> CreateSemaphoreFunc = CreateSemaphore;
        private static readonly TimeSpan LongPollingThreshold = TimeSpan.FromSeconds(1);
        private static readonly int PollingQuantumMs = 1000;
        private static readonly int DefaultPollingDelayMs = 200;
        private static readonly int MinPollingDelayMs = 100;
        private static readonly ConcurrentDictionary<Tuple<SqlServerStorage, string>, SemaphoreSlim> Semaphores =
            new ConcurrentDictionary<Tuple<SqlServerStorage, string>, SemaphoreSlim>();

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
            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"insert into [{schemaName}].JobQueue (JobId, Queue) values (@jobId, @queue)");

            using var command = ((DbConnection)connection).Create(query, timeout: _storage.CommandTimeout);
            command.AddParameter("@jobId", long.Parse(jobId, CultureInfo.InvariantCulture), DbType.Int64);
            command.AddParameter("@queue", queue, DbType.String);

#if !FEATURE_TRANSACTIONSCOPE
            command.Transaction = transaction;
#endif

            command.ExecuteNonQuery();
        }

        private SqlServerTimeoutJob DequeueUsingSlidingInvisibilityTimeout(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", nameof(queues));
            
            cancellationToken.ThrowIfCancellationRequested();

            // First we will check if our queues has any background jobs in it and
            // return if any. In this case we don't need any additional logic like
            // semaphores or waiting.
            var fetchedJob = FetchJob(queues);
            if (fetchedJob != null) return fetchedJob;

            // Then we determine whether we should use the long polling feature,
            // where only a single worker acquires a semaphore for each queue set
            // to avoid excessive load on a database.
            var configuredPollInterval = _options.QueuePollInterval;
            var useLongPolling = configuredPollInterval < LongPollingThreshold;

            // Then we determine a delay between attempts. For long-polling we use constrained
            // sub-second intervals within the [MinPollingDelayMs, PollingQuantumMs] interval.
            // For regular polling we just use the interval defined in the QueuePollInterval
            // option.
            var pollingDelayMs = useLongPolling
                ? TimeSpan.FromMilliseconds(
                    Math.Min(
                        Math.Max(
                            configuredPollInterval == TimeSpan.Zero ? DefaultPollingDelayMs : (int)configuredPollInterval.TotalMilliseconds,
                            MinPollingDelayMs),
                        PollingQuantumMs))
                : configuredPollInterval;

            var queuesString = String.Join("_", queues.OrderBy(static x => x));
            var resource = Tuple.Create(_storage, queuesString);

            using var cancellationEvent = cancellationToken.GetCancellationEvent();
            var waitArray = GetWaitArrayForQueueSignals(_storage, queues, cancellationEvent);

            SemaphoreSlim semaphore = null;

            try
            {
                semaphore = Semaphores.GetOrAdd(resource, CreateSemaphoreFunc);
                semaphore.Wait(cancellationToken);

                while (!cancellationToken.IsCancellationRequested)
                {
                    // For non-first attempts we just trying again and again with
                    // the determined delay between attempts, until shutdown
                    // request is received.
                    fetchedJob = FetchJob(queues);
                    if (fetchedJob != null) return fetchedJob;

                    WaitHandle.WaitAny(waitArray, pollingDelayMs);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                if (semaphore != null && semaphore.CurrentCount == 0)
                {
                    semaphore.Release();
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        private static SemaphoreSlim CreateSemaphore(Tuple<SqlServerStorage, string> _)
        {
            return new SemaphoreSlim(initialCount: 1);
        }

        private SqlServerTimeoutJob FetchJob(string[] queues)
        {
            return _storage.UseConnection(null, static (storage, connection, queues) =>
            {
                if (!storage.Options.SlidingInvisibilityTimeout.HasValue)
                {
                    throw new InvalidOperationException("This method should be called only when SlidingInvisibilityTimeout is set.");
                }

                var invisibilityTimeout = (int)storage.Options.SlidingInvisibilityTimeout.Value.Negate().TotalSeconds;

                using var command = CreateNonBlockingFetchCommand(storage, connection, queues, invisibilityTimeout);
                using var reader = command.ExecuteReader();

                if (!reader.Read()) return null;

                var id = reader.GetInt64(reader.GetOrdinal("Id"));
                var jobId = reader.GetInt64(reader.GetOrdinal("JobId"));
                var queue = reader.GetString(reader.GetOrdinal("Queue"));
                var fetchedAt = reader.GetDateTime(reader.GetOrdinal("FetchedAt"));

                if (reader.Read())
                {
                    throw new InvalidOperationException("Multiple rows returned from SQL Server, while expecting single or none.");
                }

                return new SqlServerTimeoutJob(storage, id, jobId.ToString(CultureInfo.InvariantCulture), queue, fetchedAt);
            }, queues);
        }

        private static DbCommand CreateNonBlockingFetchCommand(
            SqlServerStorage storage,
            DbConnection connection,
            string[] queues,
            int invisibilityTimeout)
        {
            var template = storage.GetQueryFromTemplate(static schemaName => $@"
set nocount on;set xact_abort on;set tran isolation level read committed;

update top (1) JQ
set FetchedAt = GETUTCDATE()
output INSERTED.Id, INSERTED.JobId, INSERTED.Queue, INSERTED.FetchedAt
from [{schemaName}].JobQueue JQ with (forceseek, readpast, updlock, rowlock)
where Queue in @queues and
(FetchedAt is null or FetchedAt < DATEADD(second, @timeoutSs, GETUTCDATE()));");

            return connection.Create(template, timeout: storage.CommandTimeout)
                .AddParameter("@timeoutSs", invisibilityTimeout, DbType.Int32)
                .AddExpandedParameter("@queues", queues, DbType.String);
        }

        private SqlServerTransactionJob DequeueUsingTransaction(string[] queues, CancellationToken cancellationToken)
        {
            DbTransaction transaction = null;

            var pollInterval = _options.QueuePollInterval > TimeSpan.Zero
                ? _options.QueuePollInterval
                : TimeSpan.FromSeconds(1);

            using var cancellationEvent = cancellationToken.GetCancellationEvent();
            var waitArray = GetWaitArrayForQueueSignals(_storage, queues, cancellationEvent);

            while (!cancellationToken.IsCancellationRequested)
            {
                var connection = _storage.CreateAndOpenConnection();

                try
                {
                    transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

#pragma warning disable 618
                    using var command = CreateTransactionalFetchCommand(_storage, connection, queues, (int)_options.InvisibilityTimeout.Negate().TotalSeconds);
#pragma warning restore 618
                    command.Transaction = transaction;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var jobId = reader.GetInt64(reader.GetOrdinal("JobId"));
                            var queue = reader.GetString(reader.GetOrdinal("Queue"));

                            if (reader.Read())
                            {
                                throw new InvalidOperationException(
                                    "Multiple rows returned from SQL Server, while expecting single or none.");
                            }

                            var result = new SqlServerTransactionJob(_storage, connection, transaction,
                                jobId.ToString(CultureInfo.InvariantCulture), queue);

                            // We shouldn't dispose them, because their ownership is now related
                            // to the SqlServerTransactionJob instance.
                            connection = null;
                            transaction = null;
                            return result;
                        }
                    }

                    // Nothing updated, just commit the empty transaction.
                    transaction.Commit();
                }
                catch (Exception ex) when (ex.IsCatchableExceptionType())
                {
                    // Check connection isn't broken first, and that transaction
                    // can be rolled back without throwing InvalidOperationException
                    // on older System.Data.SqlClient in .NET Core.
                    // https://github.com/HangfireIO/Hangfire/issues/1494
                    // https://github.com/dotnet/efcore/issues/12864
                    if (transaction?.Connection != null) transaction.Rollback();
                    throw;
                }
                finally
                {
                    transaction?.Dispose();
                    transaction = null;
                    _storage.ReleaseConnection(connection);
                }

                WaitHandle.WaitAny(waitArray, pollInterval);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        private static DbCommand CreateTransactionalFetchCommand(
            SqlServerStorage storage,
            DbConnection connection,
            string[] queues,
            int invisibilityTimeout)
        {
            var template = storage.GetQueryFromTemplate(static schemaName => 
                $@"delete top (1) JQ
output DELETED.Id, DELETED.JobId, DELETED.Queue
from [{schemaName}].JobQueue JQ with (readpast, updlock, rowlock, forceseek)
where Queue in @queues and (FetchedAt is null or FetchedAt < DATEADD(second, @timeout, GETUTCDATE()))");
            
            return connection
                .Create(template, timeout: storage.CommandTimeout)
                .AddParameter("@timeout", invisibilityTimeout, DbType.Int32)
                .AddExpandedParameter("@queues", queues, DbType.String);
        }

        private static WaitHandle[] GetWaitArrayForQueueSignals(SqlServerStorage storage, string[] queues, CancellationTokenExtentions.CancellationEvent cancellationEvent)
        {
            var waitList = new List<WaitHandle>(capacity: queues.Length + 1)
            {
                cancellationEvent.WaitHandle
            };

            foreach (var queue in queues)
            {
                waitList.Add(NewItemInQueueEvents.GetOrAdd(Tuple.Create(storage, queue), static _ => new AutoResetEvent(initialState: false)));
            }

            return waitList.ToArray();
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        private sealed class FetchedJob
        {
            public long Id { get; set; }
            public long JobId { get; set; }
            public string Queue { get; set; }
            public DateTime? FetchedAt { get; set; }
        }
    }
}