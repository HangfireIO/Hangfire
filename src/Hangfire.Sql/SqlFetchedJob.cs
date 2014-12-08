// // This file is part of Hangfire.
// // Copyright © 2013-2014 Sergey Odinokov.
// // 
// // Hangfire is free software: you can redistribute it and/or modify
// // it under the terms of the GNU Lesser General Public License as 
// // published by the Free Software Foundation, either version 3 
// // of the License, or any later version.
// // 
// // Hangfire is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// // GNU Lesser General Public License for more details.
// // 
// // You should have received a copy of the GNU Lesser General Public 
// // License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Data;
using Dapper;
using Hangfire.Storage;

namespace Hangfire.Sql
{
    public class SqlFetchedJob : IFetchedJob
    {
        private readonly IConnectionProvider _connectionProvider;
        protected readonly SqlBook SqlBook;

        private bool _disposed;
        private bool _removedFromQueue;
        private bool _requeued;

        public SqlFetchedJob(
            IConnectionProvider connectionProvider,
            SqlBook sqlBook,
            int id,
            string jobId,
            string queue)
        {
            if (connectionProvider == null)
            {
                throw new ArgumentNullException("connectionProvider");
            }
            if (jobId == null)
            {
                throw new ArgumentNullException("jobId");
            }
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            _connectionProvider = connectionProvider;
            SqlBook = sqlBook;
            Id = id;
            JobId = jobId;
            Queue = queue;
        }

        public int Id { get; private set; }
        public string JobId { get; private set; }
        public string Queue { get; private set; }

        public void RemoveFromQueue()
        {
            using (var connection = _connectionProvider.CreateAndOpenConnection())
            {
                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    connection.Execute(
                        SqlBook.SqlFetchedJob_RemoveFromQueue,
                        new {id = Id},
                        transaction);
                    transaction.Commit();
                }
            }
            _removedFromQueue = true;
        }

        public void Requeue()
        {
            using (var connection = _connectionProvider.CreateAndOpenConnection())
            {
                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    connection.Execute(
                        SqlBook.SqlFetchedJob_Requeue,
                        new {id = Id},
                        transaction: transaction);
                    transaction.Commit();
                }
            }
            _requeued = true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            if (!_removedFromQueue && !_requeued)
            {
                Requeue();
            }
            _disposed = true;
        }
    }
}