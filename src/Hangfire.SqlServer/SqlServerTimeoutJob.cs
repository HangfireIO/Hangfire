using System;
using Dapper;
using Hangfire.Annotations;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    internal class SqlServerTimeoutJob : IFetchedJob
    {
        private readonly SqlServerStorage _storage;
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
        }

        public long Id { get; }
        public string JobId { get; }
        public string Queue { get; }

        public void RemoveFromQueue()
        {
            _storage.UseConnection(connection =>
            {
                connection.Execute(
                    $"delete from {_storage.SchemaName}.JobQueue where Id = @id",
                    new { id = Id });
            });

            _removedFromQueue = true;
        }

        public void Requeue()
        {
            _storage.UseConnection(connection =>
            {
                connection.Execute(
                    $"update {_storage.SchemaName}.JobQueue set FetchedAt = null where Id = @id",
                    new { id = Id });
            });

            _requeued = true;
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (!_removedFromQueue && !_requeued)
            {
                Requeue();
            }

            _disposed = true;
        }
    }
}