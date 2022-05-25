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
using System.Data;
using System.Threading;
using Dapper;
using Hangfire.Annotations;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    internal class SqlServerTransactionJob : IFetchedJob
    {
        // Connections to SQL Azure Database that are idle for 30 minutes 
        // or longer will be terminated. And since we are using separate
        // connection for a holding a transaction during the background
        // job processing, we'd like to prevent Resource Governor from 
        // terminating it.
        private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromMinutes(1);

        private readonly SqlServerStorage _storage;
        private IDbConnection _connection;
        private readonly IDbTransaction _transaction;
        private readonly Timer _timer;
        private readonly object _lockObject = new object();

        public SqlServerTransactionJob(
            [NotNull] SqlServerStorage storage,
            [NotNull] IDbConnection connection, 
            [NotNull] IDbTransaction transaction, 
            string jobId, 
            string queue)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            _storage = storage;
            _connection = connection;
            _transaction = transaction;

            JobId = jobId;
            Queue = queue;

            if (!_storage.IsExistingConnection(_connection))
            {
                _timer = new Timer(ExecuteKeepAliveQuery, null, KeepAliveInterval, KeepAliveInterval);
            }
        }

        public string JobId { get; }
        public string Queue { get; }

        public void RemoveFromQueue()
        {
            lock (_lockObject)
            {
                _transaction.Commit();
            }
        }

        public void Requeue()
        {
            lock (_lockObject)
            {
                _transaction.Rollback();
            }
        }

        public void Dispose()
        {
            // Timer callback may be invoked after the Dispose method call,
            // so we are using lock to avoid unsynchronized calls.
            lock (_lockObject)
            {
                _timer?.Dispose();
                _transaction.Dispose();
                _storage.ReleaseConnection(_connection);
                _connection = null;
            }
        }

        private void ExecuteKeepAliveQuery(object obj)
        {
            lock (_lockObject)
            {
                try
                {
                    _connection?.Execute("SELECT 1;", transaction: _transaction);
                }
                catch (Exception ex) when (ex.IsCatchableExceptionType())
                {
                    // Connection was closed. So we can't continue to send
                    // keep-alive queries. Unlike for distributed locks,
                    // there is no any caveats of having this issue for
                    // queues, because Hangfire guarantees only the "at least
                    // once" processing.
                }
            }
        }
    }
}
