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

        private static readonly Func<Tuple<SqlServerStorage, string>, SemaphoreSlim> CreateSemaphoreFunc = CreateSemaphore;
        private static readonly TimeSpan LongPollingThreshold = TimeSpan.FromSeconds(1);
        private static readonly int PollingQuantumMs = 1000;
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
            
            cancellationToken.ThrowIfCancellationRequested();

            // First we will check if our queues has any background jobs in it and
            // return if any. In this case we don't need any additional logic like
            // semaphores or waiting.
            var fetchedJob = FetchJob(queues);
            if (fetchedJob != null) return fetchedJob;

            // Then we determine whether we should use the long polling feature,
            // where only a single worker acquires a semaphore for each queue set
            // to avoid excessive load on a database.
            var useLongPolling = _options.QueuePollInterval < LongPollingThreshold;

            // Then we determine a delay between attempts. For long-polling we use constrained
            // sub-second intervals within the [MinPollingDelayMs, PollingQuantumMs] interval.
            // For regular polling we just use the interval defined in the QueuePollInterval
            // option.
            var pollingDelayMs = useLongPolling
                ? TimeSpan.FromMilliseconds(
                    Math.Min(
                        Math.Max((int)_options.QueuePollInterval.TotalMilliseconds, MinPollingDelayMs),
                        PollingQuantumMs))
                : _options.QueuePollInterval;

            var queuesString = String.Join("_", queues.OrderBy(x => x));
            var resource = Tuple.Create(_storage, queuesString);

            using (var cancellationEvent = cancellationToken.GetCancellationEvent())
            {
                var waitArray = new WaitHandle[] { cancellationEvent.WaitHandle, NewItemInQueueEvent };

                SemaphoreSlim semaphore = null;

                try
                {
                    if (useLongPolling)
                    {
                        semaphore = Semaphores.GetOrAdd(resource, CreateSemaphoreFunc);
                        semaphore.Wait(cancellationToken);
                    }

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
        }

        private static SemaphoreSlim CreateSemaphore(Tuple<SqlServerStorage, string> _)
        {
            return new SemaphoreSlim(initialCount: 1);
        }

        private SqlServerTimeoutJob FetchJob(string[] queues)
        {
            return _storage.UseConnection(null, connection =>
            {
                var parameters = new DynamicParameters();
                parameters.Add("@queues", queues);
                parameters.Add("@timeoutSs", (int)_options.SlidingInvisibilityTimeout.Value.Negate().TotalSeconds);

                var fetchedJob = connection
                    .Query<FetchedJob>(
                        GetNonBlockingFetchSql(),
                        parameters,
                        commandTimeout: _storage.CommandTimeout)
                    .SingleOrDefault();
                                
                return fetchedJob != null 
                    ? new SqlServerTimeoutJob(_storage, fetchedJob.Id, fetchedJob.JobId.ToString(CultureInfo.InvariantCulture), fetchedJob.Queue, fetchedJob.FetchedAt.Value)
                    : null;
            });
        }

        private string GetNonBlockingFetchSql()
        {
            return $@"
set nocount on;set xact_abort on;set tran isolation level read committed;

update top (1) JQ
set FetchedAt = GETUTCDATE()
output INSERTED.Id, INSERTED.JobId, INSERTED.Queue, INSERTED.FetchedAt
from [{_storage.SchemaName}].JobQueue JQ with ({GetSlidingFetchTableHints()})
where Queue in @queues and
(FetchedAt is null or FetchedAt < DATEADD(second, @timeoutSs, GETUTCDATE()));";
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
                while (!cancellationToken.IsCancellationRequested)
                {
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

                    WaitHandle.WaitAny(new WaitHandle[] { cancellationEvent.WaitHandle, NewItemInQueueEvent }, pollInterval);
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                return null;
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