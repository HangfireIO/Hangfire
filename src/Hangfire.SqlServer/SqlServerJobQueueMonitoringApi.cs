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
using System.Data;
using System.Diagnostics;
using System.Linq;
using Hangfire.Annotations;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Hangfire.SqlServer
{
    internal sealed class SqlServerJobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private static readonly TimeSpan QueuesCacheTimeout = TimeSpan.FromSeconds(5);

        private readonly SqlServerStorage _storage;
        private readonly object _cacheLock = new object();

        private List<string> _queuesCache = new List<string>();
        private Stopwatch _cacheUpdated;

        public SqlServerJobQueueMonitoringApi([NotNull] SqlServerStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public IEnumerable<string> GetQueues()
        {
            lock (_cacheLock)
            {
                if (_queuesCache.Count == 0 || _cacheUpdated.Elapsed > QueuesCacheTimeout)
                {
                    var result = _storage.UseConnection(null, static (storage, connection) =>
                    {
                        var query = storage.GetQueryFromTemplate(static schemaName =>
$@"select distinct(Queue) from [{schemaName}].JobQueue with (nolock)");

                        using var command = connection.Create(query, timeout: storage.CommandTimeout);
                        return command.ExecuteList(static reader => reader.GetRequiredString("Queue"));
                    });

                    _queuesCache = result;
                    _cacheUpdated = Stopwatch.StartNew();
                }

                return _queuesCache.ToList();
            }  
        }

        public IEnumerable<long> GetEnqueuedJobIds(string queue, int from, int perPage)
        {
            return _storage.UseConnection(null, static (storage, connection, ctx) =>
            {
                var query = storage.GetQueryFromTemplate(static schemaName =>
$@"select r.JobId from (
  select jq.JobId, row_number() over (order by jq.Id) as row_num 
  from [{schemaName}].JobQueue jq with (nolock, forceseek)
  where jq.Queue = @queue and jq.FetchedAt is null
) as r
where r.row_num between @start and @end");

                using var command = connection.Create(query, timeout: storage.CommandTimeout)
                    .AddParameter("@queue", ctx.Queue, DbType.String)
                    .AddParameter("@start", ctx.From + 1, DbType.Int32)
                    .AddParameter("@end", ctx.From + ctx.PerPage, DbType.Int32);

                return command.ExecuteList(static reader => reader.GetRequiredValue<long>("JobId"));
            }, new QueuePageQueryContext(queue, from, perPage));
        }

        public IEnumerable<long> GetFetchedJobIds(string queue, int from, int perPage)
        {
            return _storage.UseConnection(null, static (storage, connection, ctx) =>
            {
                var query = storage.GetQueryFromTemplate(static schemaName => $@"
select r.JobId from (
  select jq.JobId, jq.FetchedAt, row_number() over (order by jq.Id) as row_num 
  from [{schemaName}].JobQueue jq with (nolock, forceseek)
  where jq.Queue = @queue and jq.FetchedAt is not null
) as r
where r.row_num between @start and @end");

                using var command = connection.Create(query, timeout: storage.CommandTimeout)
                    .AddParameter("@queue", ctx.Queue, DbType.String)
                    .AddParameter("@start", ctx.From + 1, DbType.Int32)
                    .AddParameter("@end", ctx.From + ctx.PerPage, DbType.Int32);

                return command.ExecuteList(static reader => reader.GetRequiredValue<long>("JobId"));
            }, new QueuePageQueryContext(queue, from, perPage));
        }

        private readonly struct QueuePageQueryContext(string queue, int from, int perPage)
        {
            public string Queue { get; } = queue;
            public int From { get; } = from;
            public int PerPage { get; } = perPage;
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            return _storage.UseConnection(null, static (storage, connection, q) =>
            {
                var query = storage.GetQueryFromTemplate(static schemaName => $@"
select sum(Enqueued) as EnqueuedCount, sum(Fetched) as FetchedCount 
from (
    select 
        case when FetchedAt is null then 1 else 0 end as Enqueued,
        case when FetchedAt is not null then 1 else 0 end as Fetched
    from [{schemaName}].JobQueue with (nolock, forceseek)
    where Queue = @queue
) q");

                using var command = connection.Create(query, timeout: storage.CommandTimeout)
                    .AddParameter("@queue", q, DbType.String);

                return command.ExecuteSingle(static reader => new EnqueuedAndFetchedCountDto
                {
                    EnqueuedCount = reader.GetOptionalValue<int?>("EnqueuedCount"),
                    FetchedCount = reader.GetOptionalValue<int?>("FetchedCount")
                });
            }, queue);
        }

        private sealed class JobIdDto
        {
            [UsedImplicitly]
            public long JobId { get; set; }
        }
    }
}