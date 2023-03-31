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
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
#if FEATURE_TRANSACTIONSCOPE
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
        private Stopwatch _cacheUpdated;

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
                if (_queuesCache.Count == 0 || _cacheUpdated.Elapsed > QueuesCacheTimeout)
                {
                    var result = _storage.UseConnection(null, connection =>
                    {
                        return connection.Query(sqlQuery, commandTimeout: _storage.CommandTimeout).Select(x => (string) x.Queue).ToList();
                    });

                    _queuesCache = result;
                    _cacheUpdated = Stopwatch.StartNew();
                }

                return _queuesCache.ToList();
            }  
        }

        public IEnumerable<long> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            var sqlQuery =
$@"select r.JobId from (
  select jq.JobId, row_number() over (order by jq.Id) as row_num 
  from [{_storage.SchemaName}].JobQueue jq with (nolock, forceseek)
  where jq.Queue = @queue and jq.FetchedAt is null
) as r
where r.row_num between @start and @end";

            return _storage.UseConnection(null, connection =>
            {
                return connection.Query<JobIdDto>(
                    sqlQuery,
                    new { queue = queue, start = from + 1, end = @from + perPage },
                    commandTimeout: _storage.CommandTimeout)
                    .ToList()
                    .Select(x => x.JobId)
                    .ToList();
            });
        }

        public IEnumerable<long> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            var fetchedJobsSql = $@"
select r.JobId from (
  select jq.JobId, jq.FetchedAt, row_number() over (order by jq.Id) as row_num 
  from [{_storage.SchemaName}].JobQueue jq with (nolock, forceseek)
  where jq.Queue = @queue and jq.FetchedAt is not null
) as r
where r.row_num between @start and @end";

            return _storage.UseConnection(null, connection =>
            {
                return connection.Query<JobIdDto>(
                        fetchedJobsSql,
                        new { queue = queue, start = from + 1, end = @from + perPage })
                    .ToList()
                    .Select(x => x.JobId)
                    .ToList();
            });
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            var sqlQuery = $@"
select sum(Enqueued) as EnqueuedCount, sum(Fetched) as FetchedCount 
from (
    select 
        case when FetchedAt is null then 1 else 0 end as Enqueued,
        case when FetchedAt is not null then 1 else 0 end as Fetched
    from [{_storage.SchemaName}].JobQueue with (nolock, forceseek)
    where Queue = @queue
) q";

            return _storage.UseConnection(null, connection =>
            {
                var result = connection.Query(sqlQuery, new { queue = queue }).Single();

                return new EnqueuedAndFetchedCountDto
                {
                    EnqueuedCount = result.EnqueuedCount,
                    FetchedCount = result.FetchedCount
                };
            });
        }

        private class JobIdDto
        {
            [UsedImplicitly]
            public long JobId { get; set; }
        }
    }
}