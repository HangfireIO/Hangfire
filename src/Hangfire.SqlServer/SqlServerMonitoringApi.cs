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
using Hangfire.Common;
using Hangfire.SqlServer.Entities;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Hangfire.SqlServer
{
    internal class SqlServerMonitoringApi : IMonitoringApi
    {
        private readonly SqlServerStorage _storage;
        private readonly int? _jobListLimit;

        public SqlServerMonitoringApi([NotNull] SqlServerStorage storage, int? jobListLimit)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            _storage = storage;
            _jobListLimit = jobListLimit;
        }

        public long ScheduledCount()
        {
            return UseConnection((connection, transaction) => 
                GetNumberOfJobsByStateName(connection, transaction, ScheduledState.StateName));
        }

        public long EnqueuedCount(string queue)
        {
            var queueApi = GetQueueApi(queue);
            var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

            return counters.EnqueuedCount ?? 0;
        }

        public long FetchedCount(string queue)
        {
            var queueApi = GetQueueApi(queue);
            var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

            return counters.FetchedCount ?? 0;
        }

        public long FailedCount()
        {
            return UseConnection((connection, transaction) => 
                GetNumberOfJobsByStateName(connection, transaction, FailedState.StateName));
        }

        public long ProcessingCount()
        {
            return UseConnection((connection, transaction) => 
                GetNumberOfJobsByStateName(connection, transaction, ProcessingState.StateName));
        }

        public JobList<ProcessingJobDto> ProcessingJobs(int @from, int count)
        {
            return UseConnection((connection, transaction) => GetJobs(
                connection,
                transaction,
                from, count,
                ProcessingState.StateName,
                (sqlJob, job, stateData) => new ProcessingJobDto
                {
                    Job = job,
                    ServerId = stateData.ContainsKey("ServerId") ? stateData["ServerId"] : stateData["ServerName"],
                    StartedAt = JobHelper.DeserializeDateTime(stateData["StartedAt"]),
                }));
        }

        public JobList<ScheduledJobDto> ScheduledJobs(int @from, int count)
        {
            return UseConnection((connection, transaction) => GetJobs(
                connection,
                transaction,
                from, count,
                ScheduledState.StateName,
                (sqlJob, job, stateData) => new ScheduledJobDto
                {
                    Job = job,
                    EnqueueAt = JobHelper.DeserializeDateTime(stateData["EnqueueAt"]),
                    ScheduledAt = JobHelper.DeserializeDateTime(stateData["ScheduledAt"])
                }));
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return UseConnection((connection, transaction) => 
                GetTimelineStats(connection, transaction, "succeeded"));
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            return UseConnection((connection, transaction) => 
                GetTimelineStats(connection, transaction, "failed"));
        }

        public IList<ServerDto> Servers()
        {
            return UseConnection<IList<ServerDto>>((connection, transaction) =>
            {
                var servers = connection.Query<Entities.Server>(
                    $@"select * from [{_storage.SchemaName}].Server",
                    transaction: transaction)
                    .ToList();

                var result = new List<ServerDto>();

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var server in servers)
                {
                    var data = JobHelper.FromJson<ServerData>(server.Data);
                    result.Add(new ServerDto
                    {
                        Name = server.Id,
                        Heartbeat = server.LastHeartbeat,
                        Queues = data.Queues,
                        StartedAt = data.StartedAt ?? DateTime.MinValue,
                        WorkersCount = data.WorkerCount
                    });
                }

                return result;
            });
        }

        public JobList<FailedJobDto> FailedJobs(int @from, int count)
        {
            return UseConnection((connection, transaction) => GetJobs(
                connection,
                transaction,
                from,
                count,
                FailedState.StateName,
                (sqlJob, job, stateData) => new FailedJobDto
                {
                    Job = job,
                    Reason = sqlJob.StateReason,
                    ExceptionDetails = stateData["ExceptionDetails"],
                    ExceptionMessage = stateData["ExceptionMessage"],
                    ExceptionType = stateData["ExceptionType"],
                    FailedAt = JobHelper.DeserializeNullableDateTime(stateData["FailedAt"])
                }));
        }

        public JobList<SucceededJobDto> SucceededJobs(int @from, int count)
        {
            return UseConnection((connection, transaction) => GetJobs(
                connection,
                transaction,
                from,
                count,
                SucceededState.StateName,
                (sqlJob, job, stateData) => new SucceededJobDto
                {
                    Job = job,
                    Result = stateData.ContainsKey("Result") ? stateData["Result"] : null,
                    TotalDuration = stateData.ContainsKey("PerformanceDuration") && stateData.ContainsKey("Latency")
                        ? (long?)long.Parse(stateData["PerformanceDuration"]) + (long?)long.Parse(stateData["Latency"])
                        : null,
                    SucceededAt = JobHelper.DeserializeNullableDateTime(stateData["SucceededAt"])
                }));
        }

        public JobList<DeletedJobDto> DeletedJobs(int @from, int count)
        {
            return UseConnection((connection, transaction) => GetJobs(
                connection,
                transaction,
                from,
                count,
                DeletedState.StateName,
                (sqlJob, job, stateData) => new DeletedJobDto
                {
                    Job = job,
                    DeletedAt = JobHelper.DeserializeNullableDateTime(stateData["DeletedAt"])
                }));
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            var tuples = _storage.QueueProviders
                .Select(x => x.GetJobQueueMonitoringApi())
                .SelectMany(x => x.GetQueues(), (monitoring, queue) => new { Monitoring = monitoring, Queue = queue })
                .OrderBy(x => x.Queue)
                .ToArray();

            var result = new List<QueueWithTopEnqueuedJobsDto>(tuples.Length);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var tuple in tuples)
            {
                var enqueuedJobIds = tuple.Monitoring.GetEnqueuedJobIds(tuple.Queue, 0, 5);
                var counters = tuple.Monitoring.GetEnqueuedAndFetchedCount(tuple.Queue);

                var firstJobs = UseConnection((connection, transaction) => 
                    EnqueuedJobs(connection, transaction, enqueuedJobIds.ToArray()));

                result.Add(new QueueWithTopEnqueuedJobsDto
                {
                    Name = tuple.Queue,
                    Length = counters.EnqueuedCount ?? 0,
                    Fetched = counters.FetchedCount,
                    FirstJobs = firstJobs
                });
            }

            return result;
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int @from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var enqueuedJobIds = queueApi.GetEnqueuedJobIds(queue, from, perPage);

            return UseConnection((connection, transaction) => EnqueuedJobs(connection, transaction, enqueuedJobIds.ToArray()));
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var fetchedJobIds = queueApi.GetFetchedJobIds(queue, from, perPage);

            return UseConnection((connection, transaction) => FetchedJobs(connection, transaction, fetchedJobIds));
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return UseConnection((connection, transaction) => 
                GetHourlyTimelineStats(connection, transaction, "succeeded"));
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return UseConnection((connection, transaction) => 
                GetHourlyTimelineStats(connection, transaction, "failed"));
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            return UseConnection((connection, transaction) =>
            {

                string sql = $@"
select * from [{_storage.SchemaName}].Job where Id = @id
select * from [{_storage.SchemaName}].JobParameter where JobId = @id
select * from [{_storage.SchemaName}].State where JobId = @id order by Id desc";

                using (var multi = connection.QueryMultiple(sql, new { id = jobId }, transaction))
                {
                    var job = multi.Read<SqlJob>().SingleOrDefault();
                    if (job == null) return null;

                    var parameters = multi.Read<JobParameter>().ToDictionary(x => x.Name, x => x.Value);
                    var history =
                        multi.Read<SqlState>()
                            .ToList()
                            .Select(x => new StateHistoryDto
                            {
                                StateName = x.Name,
                                CreatedAt = x.CreatedAt,
                                Reason = x.Reason,
                                Data = new Dictionary<string, string>(
                                    JobHelper.FromJson<Dictionary<string, string>>(x.Data),
                                    StringComparer.OrdinalIgnoreCase),
                            })
                            .ToList();

                    return new JobDetailsDto
                    {
                        CreatedAt = job.CreatedAt,
                        ExpireAt = job.ExpireAt,
                        Job = DeserializeJob(job.InvocationData, job.Arguments),
                        History = history,
                        Properties = parameters
                    };
                }
            });
        }

        public long SucceededListCount()
        {
            return UseConnection((connection, transaction) => 
                GetNumberOfJobsByStateName(connection, transaction, SucceededState.StateName));
        }

        public long DeletedListCount()
        {
            return UseConnection((connection, transaction) => 
                GetNumberOfJobsByStateName(connection, transaction, DeletedState.StateName));
        }

        public StatisticsDto GetStatistics()
        {
            string sql = String.Format(@"
select count(Id) from [{0}].Job where StateName = N'Enqueued';
select count(Id) from [{0}].Job where StateName = N'Failed';
select count(Id) from [{0}].Job where StateName = N'Processing';
select count(Id) from [{0}].Job where StateName = N'Scheduled';
select count(Id) from [{0}].Server;
select sum(s.[Value]) from (
    select sum([Value]) as [Value] from [{0}].Counter where [Key] = N'stats:succeeded'
    union all
    select [Value] from [{0}].AggregatedCounter where [Key] = N'stats:succeeded'
) as s;
select sum(s.[Value]) from (
    select sum([Value]) as [Value] from [{0}].Counter where [Key] = N'stats:deleted'
    union all
    select [Value] from [{0}].AggregatedCounter where [Key] = N'stats:deleted'
) as s;
select count(*) from [{0}].[Set] where [Key] = N'recurring-jobs';
", _storage.SchemaName);

            var statistics = UseConnection((connection, transaction) =>
            {
                var stats = new StatisticsDto();
                using (var multi = connection.QueryMultiple(sql, transaction: transaction))
                {
                    stats.Enqueued = multi.Read<int>().Single();
                    stats.Failed = multi.Read<int>().Single();
                    stats.Processing = multi.Read<int>().Single();
                    stats.Scheduled = multi.Read<int>().Single();

                    stats.Servers = multi.Read<int>().Single();

                    stats.Succeeded = multi.Read<long?>().SingleOrDefault() ?? 0;
                    stats.Deleted = multi.Read<long?>().SingleOrDefault() ?? 0;

                    stats.Recurring = multi.Read<int>().Single();
                }
                return stats;
            });

            statistics.Queues = _storage.QueueProviders
                .SelectMany(x => x.GetJobQueueMonitoringApi().GetQueues())
                .Count();

            return statistics;
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(
            DbConnection connection,
            DbTransaction transaction,
            string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => $"stats:{type}:{x.ToString("yyyy-MM-dd-HH")}", x => x);

            return GetTimelineStats(connection, transaction, keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(
            DbConnection connection,
            DbTransaction transaction,
            string type)
        {
            var endDate = DateTime.UtcNow.Date;
            var dates = new List<DateTime>();
            for (var i = 0; i < 7; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddDays(-1);
            }

            var keyMaps = dates.ToDictionary(x => $"stats:{type}:{x.ToString("yyyy-MM-dd")}", x => x);

            return GetTimelineStats(connection, transaction, keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(
            DbConnection connection,
            DbTransaction transaction,
            IDictionary<string, DateTime> keyMaps)
        {
            string sqlQuery =
$@"select [Key], [Value] as [Count] from [{_storage.SchemaName}].AggregatedCounter
where [Key] in @keys";

            var valuesMap = connection.Query(
                sqlQuery,
                new { keys = keyMaps.Keys },
                transaction)
                .ToDictionary(x => (string)x.Key, x => (long)x.Count);

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);
            }

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }

            return result;
        }

        private IPersistentJobQueueMonitoringApi GetQueueApi(string queueName)
        {
            var provider = _storage.QueueProviders.GetProvider(queueName);
            var monitoringApi = provider.GetJobQueueMonitoringApi();

            return monitoringApi;
        }

        private T UseConnection<T>(Func<DbConnection, DbTransaction, T> action)
        {
            return _storage.UseTransaction(action, IsolationLevel.ReadUncommitted);
        }

        private JobList<EnqueuedJobDto> EnqueuedJobs(
            DbConnection connection,
            DbTransaction transaction,
            int[] jobIds)
        {
            string enqueuedJobsSql = 
$@"select j.*, s.Reason as StateReason, s.Data as StateData 
from [{_storage.SchemaName}].Job j
left join [{_storage.SchemaName}].State s on s.Id = j.StateId
where j.Id in @jobIds";

            var jobs = connection.Query<SqlJob>(
                enqueuedJobsSql,
                new { jobIds = jobIds },
                transaction)
                .ToDictionary(x => x.Id, x => x);

            var sortedSqlJobs = jobIds
                .Select(jobId => jobs.ContainsKey(jobId) ? jobs[jobId] : new SqlJob { Id = jobId })
                .ToList();
            
            return DeserializeJobs(
                sortedSqlJobs,
                (sqlJob, job, stateData) => new EnqueuedJobDto
                {
                    Job = job,
                    State = sqlJob.StateName,
                    EnqueuedAt = sqlJob.StateName == EnqueuedState.StateName
                        ? JobHelper.DeserializeNullableDateTime(stateData["EnqueuedAt"])
                        : null
                });
        }

        private long GetNumberOfJobsByStateName(DbConnection connection, DbTransaction transaction, string stateName)
        {
            var sqlQuery = _jobListLimit.HasValue
                ? $@"select count(j.Id) from (select top (@limit) Id from [{_storage.SchemaName}].Job where StateName = @state) as j"
                : $@"select count(Id) from [{_storage.SchemaName}].Job where StateName = @state";

            var count = connection.Query<int>(
                 sqlQuery,
                 new { state = stateName, limit = _jobListLimit },
                 transaction)
                 .Single();

            return count;
        }

        private static Job DeserializeJob(string invocationData, string arguments)
        {
            var data = JobHelper.FromJson<InvocationData>(invocationData);
            data.Arguments = arguments;

            try
            {
                return data.Deserialize();
            }
            catch (JobLoadException)
            {
                return null;
            }
        }

        private JobList<TDto> GetJobs<TDto>(
            DbConnection connection,
            DbTransaction transaction,
            int from,
            int count,
            string stateName,
            Func<SqlJob, Job, Dictionary<string, string>, TDto> selector)
        {
            string jobsSql = 
$@"select * from (
  select j.*, s.Reason as StateReason, s.Data as StateData, row_number() over (order by j.Id desc) as row_num
  from [{_storage.SchemaName}].Job j with (forceseek)
  left join [{_storage.SchemaName}].State s on j.StateId = s.Id
  where j.StateName = @stateName
) as j where j.row_num between @start and @end";

            var jobs = connection.Query<SqlJob>(
                        jobsSql,
                        new { stateName = stateName, start = @from + 1, end = @from + count },
                        transaction)
                        .ToList();

            return DeserializeJobs(jobs, selector);
        }

        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<SqlJob> jobs,
            Func<SqlJob, Job, Dictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);
            
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var job in jobs)
            {
                var dto = default(TDto);
                
                if (job.InvocationData != null)
                {
                    var deserializedData = JobHelper.FromJson<Dictionary<string, string>>(job.StateData);
                    var stateData = deserializedData != null
                        ? new Dictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                        : null;

                    dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments), stateData);
                }

                result.Add(new KeyValuePair<string, TDto>(
                    job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
        }

        private JobList<FetchedJobDto> FetchedJobs(
            DbConnection connection,
            DbTransaction transaction,
            IEnumerable<int> jobIds)
        {
            string fetchedJobsSql = 
$@"select j.*, s.Reason as StateReason, s.Data as StateData 
from [{_storage.SchemaName}].Job j
left join [{_storage.SchemaName}].State s on s.Id = j.StateId
where j.Id in @jobIds";

            var jobs = connection.Query<SqlJob>(
                fetchedJobsSql,
                new { jobIds = jobIds },
                transaction)
                .ToList();

            var result = new List<KeyValuePair<string, FetchedJobDto>>(jobs.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var job in jobs)
            {
                result.Add(new KeyValuePair<string, FetchedJobDto>(
                    job.Id.ToString(),
                    new FetchedJobDto
                    {
                        Job = DeserializeJob(job.InvocationData, job.Arguments),
                        State = job.StateName,
                    }));
            }

            return new JobList<FetchedJobDto>(result);
        }
    }
}

