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
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(SqlServerTimeoutJob));

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
            [NotNull] string queue)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            _storage = storage;

            Id = id;
            JobId = jobId;
            Queue = queue;

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

        public void RemoveFromQueue()
        {
            lock (_syncRoot)
            {
                _storage.UseConnection(null, connection =>
                {
                    connection.Execute(
                        $"delete from {_storage.SchemaName}.JobQueue where Id = @id",
                        new { id = Id },
                        commandTimeout: _storage.CommandTimeout);
                });

                _removedFromQueue = true;
            }
        }

        public void Requeue()
        {
            lock (_syncRoot)
            {
                _storage.UseConnection(null, connection =>
                {
                    connection.Execute(
                        $"update {_storage.SchemaName}.JobQueue set FetchedAt = null where Id = @id",
                        new { id = Id },
                        commandTimeout: _storage.CommandTimeout);
                });

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

        private void ExecuteKeepAliveQuery(object obj)
        {
            lock (_syncRoot)
            {
                if (_requeued || _removedFromQueue) return;

                try
                {
                    _storage.UseConnection(null, connection =>
                    {
                        connection.Execute(
                            $"update {_storage.SchemaName}.JobQueue set FetchedAt = getutcdate() where Id = @id",
                            new { id = Id },
                            commandTimeout: _storage.CommandTimeout);
                    });

                    Logger.Trace($"Keep-alive query for message {Id} sent");
                }
                catch (Exception ex)
                {
                    Logger.DebugException($"Unable to execute keep-alive query for message {Id}", ex);
                }
            }
        }
    }
}