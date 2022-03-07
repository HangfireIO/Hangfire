// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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