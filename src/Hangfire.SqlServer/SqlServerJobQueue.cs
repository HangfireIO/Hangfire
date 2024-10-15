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
using Dapper;
using Hangfire.Annotations;
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

            connection.Execute(
                query, 
                new { jobId = long.Parse(jobId, CultureInfo.InvariantCulture), queue = queue }
#if !FEATURE_TRANSACTIONSCOPE
                , transaction
#endif
                , commandTimeout: _storage.CommandTimeout);
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

            var waitArray = GetWaitArrayForQueueSignals(_storage, queues, cancellationToken);

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
                var fetchedJob = connection
                    .QuerySingleOrDefault<FetchedJob>(
                        GetNonBlockingFetchSql(storage),
                        new { queues = queues, timeoutSs = (int)storage.Options.SlidingInvisibilityTimeout.Value.Negate().TotalSeconds },
                        commandTimeout: storage.CommandTimeout);

                return fetchedJob != null 
                    ? new SqlServerTimeoutJob(storage, fetchedJob.Id, fetchedJob.JobId.ToString(CultureInfo.InvariantCulture), fetchedJob.Queue, fetchedJob.FetchedAt.Value)
                    : null;
            }, queues);
        }

        private static string GetNonBlockingFetchSql(SqlServerStorage storage)
        {
            return storage.GetQueryFromTemplate(static schemaName => $@"
set nocount on;set xact_abort on;set tran isolation level read committed;

update top (1) JQ
set FetchedAt = GETUTCDATE()
output INSERTED.Id, INSERTED.JobId, INSERTED.Queue, INSERTED.FetchedAt
from [{schemaName}].JobQueue JQ with (forceseek, readpast, updlock, rowlock)
where Queue in @queues and
(FetchedAt is null or FetchedAt < DATEADD(second, @timeoutSs, GETUTCDATE()));");
        }

        private SqlServerTransactionJob DequeueUsingTransaction(string[] queues, CancellationToken cancellationToken)
        {
            FetchedJob fetchedJob = null;
            DbTransaction transaction = null;

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"delete top (1) JQ
output DELETED.Id, DELETED.JobId, DELETED.Queue
from [{schemaName}].JobQueue JQ with (readpast, updlock, rowlock, forceseek)
where Queue in @queues and (FetchedAt is null or FetchedAt < DATEADD(second, @timeout, GETUTCDATE()))");

            var pollInterval = _options.QueuePollInterval > TimeSpan.Zero
                ? _options.QueuePollInterval
                : TimeSpan.FromSeconds(1);

            var waitArray = GetWaitArrayForQueueSignals(_storage, queues, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var connection = _storage.CreateAndOpenConnection();

                try
                {
                    transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                    fetchedJob = connection.QuerySingleOrDefault<FetchedJob>(
                        query,
#pragma warning disable 618
                    new { queues = queues, timeout = _options.InvisibilityTimeout.Negate().TotalSeconds },
#pragma warning restore 618
                    transaction,
                        commandTimeout: _storage.CommandTimeout);

                    if (fetchedJob != null)
                    {
                        return new SqlServerTransactionJob(
                            _storage,
                            connection,
                            transaction,
                            fetchedJob.JobId.ToString(CultureInfo.InvariantCulture),
                            fetchedJob.Queue);
                    }
                    else
                    {
                        // Nothing updated, just commit the empty transaction.
                        transaction.Commit();
                    }
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
                    if (fetchedJob == null)
                    {
                        transaction?.Dispose();
                        transaction = null;

                        _storage.ReleaseConnection(connection);
                    }
                }

                WaitHandle.WaitAny(waitArray, pollInterval);
            }
                
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        private static WaitHandle[] GetWaitArrayForQueueSignals(SqlServerStorage storage, string[] queues, CancellationToken cancellationToken)
        {
            var waitList = new List<WaitHandle>(capacity: queues.Length + 1)
            {
                cancellationToken.WaitHandle
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