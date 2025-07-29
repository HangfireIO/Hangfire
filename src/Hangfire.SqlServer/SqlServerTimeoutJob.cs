// This file is part of Hangfire. Copyright © 2017 Hangfire OÜ.
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
using System.Threading;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    internal sealed class SqlServerTimeoutJob : IFetchedJob
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(SqlServerTimeoutJob));

        private readonly object _syncRoot = new object();
        private readonly SqlServerStorage _storage;
        private bool _disposed;
        private bool _removedFromQueue;
        private bool _requeued;
        private SqlServerWriteOnlyTransaction? _transaction;

        private long _lastHeartbeat;
        private TimeSpan _interval;

        public SqlServerTimeoutJob(
            SqlServerStorage storage,
            long id,
            string jobId,
            string queue,
            DateTime fetchedAt)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            Id = id;
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            FetchedAt = fetchedAt;

            if (storage.SlidingInvisibilityTimeout.HasValue)
            {
                _lastHeartbeat = TimestampHelper.GetTimestamp();
                _interval = TimeSpan.FromSeconds(storage.SlidingInvisibilityTimeout.Value.TotalSeconds / 5);
                storage.HeartbeatProcess.Track(this);
            }
        }

        public long Id { get; }
        public string JobId { get; }
        public string Queue { get; }

        internal DateTime? FetchedAt { get; private set; }

        public void RemoveFromQueue()
        {
            lock (_syncRoot)
            {
                if (_transaction != null && _transaction.Committed) return;
                if (!FetchedAt.HasValue) return;

                _storage.UseConnection(
                    static (storage, connection, ctx) =>
                    {
                        var query = storage.GetQueryFromTemplate(static schemaName =>
                            $@"delete JQ from [{schemaName}].JobQueue JQ with (forceseek, rowlock) where Queue = @queue and Id = @id and FetchedAt = @fetchedAt");

                        using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout)
                            .AddParameter("@queue", ctx.Queue, DbType.String)
                            .AddParameter("@id", ctx.Id, DbType.Int64)
                            .AddParameter("@fetchedAt", ctx.FetchedAt, DbType.DateTime);

                        return command.ExecuteNonQuery();
                    },
                    this);

                _removedFromQueue = true;
            }
        }

        public void Requeue()
        {
            lock (_syncRoot)
            {
                if (_transaction != null && _transaction.Committed) return;

                if (!FetchedAt.HasValue) return;

                _storage.UseConnection(
                    static (storage, connection, ctx) =>
                    {
                        var query = storage.GetQueryFromTemplate(static schemaName =>
                            $@"update JQ set FetchedAt = null from [{schemaName}].JobQueue JQ with (forceseek, rowlock) where Queue = @queue and Id = @id and FetchedAt = @fetchedAt");

                        using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout)
                            .AddParameter("@queue", ctx.Queue, DbType.String)
                            .AddParameter("@id", ctx.Id, DbType.Int64)
                            .AddParameter("@fetchedAt", ctx.FetchedAt, DbType.DateTime);

                        return command.ExecuteNonQuery();
                    },
                    this);

                FetchedAt = null;
                _requeued = true;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            DisposeTimer();

            lock (_syncRoot)
            {
                if (!_removedFromQueue && !_requeued && (_transaction == null || !_transaction.Committed))
                {
                    Requeue();
                }
            }
        }

        internal void SetTransaction(SqlServerWriteOnlyTransaction transaction)
        {
            lock (_syncRoot)
            {
                _transaction = transaction;
            }
        }

        internal void DisposeTimer()
        {
            _storage.HeartbeatProcess.Untrack(this);
        }

        internal void ExecuteKeepAliveQueryIfRequired()
        {
            var now = TimestampHelper.GetTimestamp();

            if (TimestampHelper.Elapsed(now, Interlocked.Read(ref _lastHeartbeat)) >= _interval)
            {
                lock (_syncRoot)
                {
                    if (!FetchedAt.HasValue) return;

                    if (_requeued || _removedFromQueue) return;
                    if (_transaction != null && _transaction.Committed) return;

                    try
                    {
                        FetchedAt = _storage.UseConnection(
                            static (storage, connection, ctx) =>
                            {
                                var query = storage.GetQueryFromTemplate(static schemaName =>
                                    $@"update JQ set FetchedAt = getutcdate() output INSERTED.FetchedAt from [{schemaName}].JobQueue JQ with (forceseek, rowlock) where Queue = @queue and Id = @id and FetchedAt = @fetchedAt");

                                using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout)
                                    .AddParameter("@queue", ctx.Queue, DbType.String)
                                    .AddParameter("@id", ctx.Id, DbType.Int64)
                                    .AddParameter("@fetchedAt", ctx.FetchedAt, DbType.DateTime);

                                return command.ExecuteScalar<DateTime?>();
                            },
                            this);

                        if (!FetchedAt.HasValue)
                        {
                            _logger.Warn(
                                $"Background job identifier '{JobId}' was fetched by another worker, will not execute keep alive.");
                        }

                        _logger.Trace($"Keep-alive query for message {Id} sent");
                        Interlocked.Exchange(ref _lastHeartbeat, now);
                    }
                    catch (Exception ex) when (ex.IsCatchableExceptionType())
                    {
                        _logger.DebugException($"Unable to execute keep-alive query for message {Id}", ex);
                    }
                }
            }
        }
    }
}