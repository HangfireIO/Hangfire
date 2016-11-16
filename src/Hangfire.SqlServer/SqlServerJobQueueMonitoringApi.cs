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
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
#if NETFULL
using System.Transactions;
#else
using System.Data;
#endif
using Dapper;
using Hangfire.Annotations;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Hangfire.SqlServer
{
    internal class SqlServerJobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private static readonly TimeSpan QueuesCacheTimeout = TimeSpan.FromSeconds(5);

        private readonly SqlServerStorage _storage;
        private readonly object _cacheLock = new object();

        private List<string> _queuesCache = new List<string>();
        private DateTime _cacheUpdated;

        public SqlServerJobQueueMonitoringApi([NotNull] SqlServerStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            _storage = storage;
        }

        public IEnumerable<string> GetQueues()
        {
            string sqlQuery = $@"select distinct(Queue) from [{_storage.SchemaName}].JobQueue with (nolock)";

            lock (_cacheLock)
            {
                if (_queuesCache.Count == 0 || _cacheUpdated.Add(QueuesCacheTimeout) < DateTime.UtcNow)
                {
                    var result = UseTransaction((connection, transaction) =>
                    {
                        return connection.Query(sqlQuery, transaction: transaction).Select(x => (string) x.Queue).ToList();
                    });

                    _queuesCache = result;
                    _cacheUpdated = DateTime.UtcNow;
                }

                return _queuesCache.ToList();
            }  
        }

        public IEnumerable<int> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            string sqlQuery =
$@"select r.JobId from (
  select jq.JobId, row_number() over (order by jq.Id) as row_num 
  from [{_storage.SchemaName}].JobQueue jq with (nolock)
  where jq.Queue = @queue
) as r
where r.row_num between @start and @end";

            return UseTransaction((connection, transaction) =>
            {
                return connection.Query<JobIdDto>(
                    sqlQuery,
                    new { queue = queue, start = from + 1, end = @from + perPage },
                    transaction)
                    .ToList()
                    .Select(x => x.JobId)
                    .ToList();
            });
        }

        public IEnumerable<int> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            return Enumerable.Empty<int>();
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            string sqlQuery = $@"
select count(Id) from [{_storage.SchemaName}].JobQueue with (nolock) where [Queue] = @queue";

            return UseTransaction((connection, transaction) =>
            {
                var result = connection.ExecuteScalar<int>(sqlQuery, new { queue = queue }, transaction);

                return new EnqueuedAndFetchedCountDto
                {
                    EnqueuedCount = result,
                };
            });
        }

        private T UseTransaction<T>(Func<DbConnection, DbTransaction, T> func)
        {
            return _storage.UseTransaction(func, IsolationLevel.ReadUncommitted);
        }

        private class JobIdDto
        {
            [UsedImplicitly]
            public int JobId { get; set; }
        }
    }
}