using System;
using System.Threading;
using Dapper;
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    internal class SqlServerTimeoutJob : IFetchedJob
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(SqlServerTimeoutJob));

        private readonly object _syncRoot = new object();
        private readonly SqlServerStorage _storage;
        private readonly Timer _timer;
        private bool _disposed;
        private bool _removedFromQueue;
        private bool _requeued;

        public SqlServerTimeoutJob(
            [NotNull] SqlServerStorage storage,
            long id,
            [NotNull] string jobId,
            [NotNull] string queue,
            [NotNull] DateTime? fetchedAt)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            if (fetchedAt == null) throw new ArgumentNullException(nameof(fetchedAt));

            _storage = storage;

            Id = id;
            JobId = jobId;
            Queue = queue;
            FetchedAt = fetchedAt.Value;

            if (storage.SlidingInvisibilityTimeout.HasValue)
            {
                var keepAliveInterval =
                    TimeSpan.FromSeconds(storage.SlidingInvisibilityTimeout.Value.TotalSeconds / 5);
                _timer = new Timer(ExecuteKeepAliveQuery, null, keepAliveInterval, keepAliveInterval);
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
                if (!FetchedAt.HasValue) return;

                _storage.UseConnection(null, connection =>
                {
                    connection.Execute(
                        $"delete JQ from [{_storage.SchemaName}].JobQueue JQ with ({GetTableHints()}) where Queue = @queue and Id = @id and FetchedAt = @fetchedAt",
                        new { queue = Queue, id = Id, fetchedAt = FetchedAt },
                        commandTimeout: _storage.CommandTimeout);
                });

                _removedFromQueue = true;
            }
        }

        public void Requeue()
        {
            lock (_syncRoot)
            {
                if (!FetchedAt.HasValue) return;

                _storage.UseConnection(null, connection =>
                {
                    connection.Execute(
                        $"update JQ set FetchedAt = null from [{_storage.SchemaName}].JobQueue JQ with ({GetTableHints()}) where Queue = @queue and Id = @id and FetchedAt = @fetchedAt",
                        new { queue = Queue, id = Id, fetchedAt = FetchedAt },
                        commandTimeout: _storage.CommandTimeout);
                });

                FetchedAt = null;
                _requeued = true;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timer?.Dispose();

            lock (_syncRoot)
            {
                if (!_removedFromQueue && !_requeued)
                {
                    Requeue();
                }
            }
        }

        private string GetTableHints()
        {
            if (_storage.Options.UsePageLocksOnDequeue)
            {
                return "forceseek, paglock, xlock";
            }

            return "forceseek, rowlock";
        }

        private void ExecuteKeepAliveQuery(object obj)
        {
            lock (_syncRoot)
            {
                if (!FetchedAt.HasValue) return;

                if (_requeued || _removedFromQueue) return;

                try
                {
                    _storage.UseConnection(null, connection =>
                    {
                        FetchedAt = connection.ExecuteScalar<DateTime?>(
                            $"update JQ set FetchedAt = getutcdate() output INSERTED.FetchedAt from [{_storage.SchemaName}].JobQueue JQ with ({GetTableHints()}) where Queue = @queue and Id = @id and FetchedAt = @fetchedAt",
                            new { queue = Queue, id = Id, fetchedAt = FetchedAt },
                            commandTimeout: _storage.CommandTimeout);
                    });

                    if (!FetchedAt.HasValue)
                    {
                        _logger.Warn($"Background job identifier '{JobId}' was fetched by another worker, will not execute keep alive.");
                    }

                    _logger.Trace($"Keep-alive query for message {Id} sent");
                }
                catch (Exception ex)
                {
                    _logger.DebugException($"Unable to execute keep-alive query for message {Id}", ex);
                }
            }
        }
    }
}