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
            return UseConnection(connection => 
                GetNumberOfJobsByStateName(connection, ScheduledState.StateName));
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
            return UseConnection(connection => 
                GetNumberOfJobsByStateName(connection, FailedState.StateName));
        }

        public long ProcessingCount()
        {
            return UseConnection(connection => 
                GetNumberOfJobsByStateName(connection, ProcessingState.StateName));
        }

        public JobList<ProcessingJobDto> ProcessingJobs(int @from, int count)
        {
            return UseConnection(connection => GetJobs(
                connection,
                from, count,
                ProcessingState.StateName,
                (sqlJob, job, stateData) => new ProcessingJobDto
                {
                    Job = job,
                    InProcessingState = ProcessingState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    ServerId = stateData.ContainsKey("ServerId") ? stateData["ServerId"] : stateData["ServerName"],
                    StartedAt = sqlJob.StateChanged,
                }));
        }

        public JobList<ScheduledJobDto> ScheduledJobs(int @from, int count)
        {
            return UseConnection(connection => GetJobs(
                connection,
                from, count,
                ScheduledState.StateName,
                (sqlJob, job, stateData) => new ScheduledJobDto
                {
                    Job = job,
                    InScheduledState = ScheduledState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    EnqueueAt = JobHelper.DeserializeNullableDateTime(stateData["EnqueueAt"]) ?? DateTime.MinValue,
                    ScheduledAt = sqlJob.StateChanged
                }));
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return UseConnection(connection => 
                GetTimelineStats(connection, "succeeded"));
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            return UseConnection(connection => 
                GetTimelineStats(connection, "failed"));
        }

        public IList<ServerDto> Servers()
        {
            return UseConnection<IList<ServerDto>>(connection =>
            {
                var servers = connection.Query<Entities.Server>(
                    $@"select * from [{_storage.SchemaName}].Server with (nolock)", commandTimeout: _storage.CommandTimeout)
                    .ToList();

                var result = new List<ServerDto>();

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var server in servers)
                {
                    var data = SerializationHelper.Deserialize<ServerData>(server.Data);

                    if (data.Queues == null && data.StartedAt == null && data.WorkerCount == 0)
                    {
                        data = SerializationHelper.Deserialize<ServerData>(server.Data, SerializationOption.User);
                    }

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
            return UseConnection(connection => GetJobs(
                connection,
                from,
                count,
                FailedState.StateName,
                (sqlJob, job, stateData) => new FailedJobDto
                {
                    Job = job,
                    InFailedState = FailedState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    Reason = sqlJob.StateReason,
                    ExceptionDetails = stateData["ExceptionDetails"],
                    ExceptionMessage = stateData["ExceptionMessage"],
                    ExceptionType = stateData["ExceptionType"],
                    FailedAt = sqlJob.StateChanged
                }));
        }

        public JobList<SucceededJobDto> SucceededJobs(int @from, int count)
        {
            return UseConnection(connection => GetJobs(
                connection,
                from,
                count,
                SucceededState.StateName,
                (sqlJob, job, stateData) => new SucceededJobDto
                {
                    Job = job,
                    InSucceededState = SucceededState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    Result = stateData["Result"],
                    TotalDuration = stateData.ContainsKey("PerformanceDuration") && stateData.ContainsKey("Latency")
                        ? (long?)long.Parse(stateData["PerformanceDuration"]) + (long?)long.Parse(stateData["Latency"])
                        : null,
                    SucceededAt = sqlJob.StateChanged
                }));
        }

        public JobList<DeletedJobDto> DeletedJobs(int @from, int count)
        {
            return UseConnection(connection => GetJobs(
                connection,
                from,
                count,
                DeletedState.StateName,
                (sqlJob, job, stateData) => new DeletedJobDto
                {
                    Job = job,
                    InDeletedState = DeletedState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    DeletedAt = sqlJob.StateChanged
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

                var firstJobs = UseConnection(connection => 
                    EnqueuedJobs(connection, enqueuedJobIds.ToArray()));

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

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var enqueuedJobIds = queueApi.GetEnqueuedJobIds(queue, from, perPage);

            return UseConnection(connection => EnqueuedJobs(connection, enqueuedJobIds.ToArray()));
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var fetchedJobIds = queueApi.GetFetchedJobIds(queue, from, perPage);

            return UseConnection(connection => FetchedJobs(connection, fetchedJobIds.ToArray()));
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return UseConnection(connection => 
                GetHourlyTimelineStats(connection, "succeeded"));
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return UseConnection(connection => 
                GetHourlyTimelineStats(connection, "failed"));
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            return UseConnection(connection =>
            {
                string sql = $@"
select * from [{_storage.SchemaName}].Job with (nolock, forceseek) where Id = @id
select * from [{_storage.SchemaName}].JobParameter with (nolock, forceseek) where JobId = @id
select * from [{_storage.SchemaName}].State with (nolock, forceseek) where JobId = @id order by Id desc";

                using (var multi = connection.QueryMultiple(sql, new { id = jobId }, commandTimeout: _storage.CommandTimeout))
                {
                    var job = multi.Read<SqlJob>().SingleOrDefault();
                    if (job == null) return null;

                    var parameters = multi.Read<JobParameter>()
                        .GroupBy(x => x.Name)
                        .Select(grp => grp.First())
                        .ToDictionary(x => x.Name, x => x.Value);

                    var deserializedJob = DeserializeJob(job.InvocationData, job.Arguments, out var payload, out var exception);

                    if (deserializedJob == null)
                    {
                        if (payload != null)
                        {
                            parameters.Add("DBG_Type", payload.Type);
                            parameters.Add("DBG_Method", payload.Method);
                            parameters.Add("DBG_Args", payload.Arguments);
                        }
                        else
                        {
                            parameters.Add("DBG_Payload", job.InvocationData);
                            parameters.Add("DBG_Args", job.Arguments);
                        }
                    }

                    if (exception != null)
                    {
                        parameters.Add("DBG_Exception", (exception.InnerException ?? exception).Message);
                    }

                    var history =
                        multi.Read<SqlState>()
                            .ToList()
                            .Select(x => new StateHistoryDto
                            {
                                StateName = x.Name,
                                CreatedAt = x.CreatedAt,
                                Reason = x.Reason,
                                Data = new SafeDictionary<string, string>(
                                    SerializationHelper.Deserialize<Dictionary<string, string>>(x.Data),
                                    StringComparer.OrdinalIgnoreCase),
                            })
                            .ToList();

                    return new JobDetailsDto
                    {
                        CreatedAt = job.CreatedAt,
                        ExpireAt = job.ExpireAt,
                        Job = deserializedJob,
                        History = history,
                        Properties = parameters
                    };
                }
            });
        }

        public long SucceededListCount()
        {
            return UseConnection(connection => 
                GetNumberOfJobsByStateName(connection, SucceededState.StateName));
        }

        public long DeletedListCount()
        {
            return UseConnection(connection => 
                GetNumberOfJobsByStateName(connection, DeletedState.StateName));
        }

        public StatisticsDto GetStatistics()
        {
            string sql = String.Format(@"
set transaction isolation level read committed;
select count(Id) from [{0}].Job with (nolock, forceseek) where StateName = N'Enqueued';
select count(Id) from [{0}].Job with (nolock, forceseek) where StateName = N'Failed';
select count(Id) from [{0}].Job with (nolock, forceseek) where StateName = N'Processing';
select count(Id) from [{0}].Job with (nolock, forceseek) where StateName = N'Scheduled';
select count(Id) from [{0}].Server with (nolock);
select sum(s.[Value]) from (
    select sum([Value]) as [Value] from [{0}].Counter with (nolock, forceseek) where [Key] = N'stats:succeeded'
    union all
    select [Value] from [{0}].AggregatedCounter with (nolock, forceseek) where [Key] = N'stats:succeeded'
) as s;
select sum(s.[Value]) from (
    select sum([Value]) as [Value] from [{0}].Counter with (nolock, forceseek) where [Key] = N'stats:deleted'
    union all
    select [Value] from [{0}].AggregatedCounter with (nolock, forceseek) where [Key] = N'stats:deleted'
) as s;

select count(*) from [{0}].[Set] with (nolock, forceseek) where [Key] = N'recurring-jobs';
                ", _storage.SchemaName);

            var statistics = UseConnection(connection =>
            {
                var stats = new StatisticsDto();
                using (var multi = connection.QueryMultiple(sql, commandTimeout: _storage.CommandTimeout))
                {
                    stats.Enqueued = multi.ReadSingle<int>();
                    stats.Failed = multi.ReadSingle<int>();
                    stats.Processing = multi.ReadSingle<int>();
                    stats.Scheduled = multi.ReadSingle<int>();

                    stats.Servers = multi.ReadSingle<int>();

                    stats.Succeeded = multi.ReadSingleOrDefault<long?>() ?? 0;
                    stats.Deleted = multi.ReadSingleOrDefault<long?>() ?? 0;

                    stats.Recurring = multi.ReadSingle<int>();
                }
                return stats;
            });

            statistics.Queues = _storage.QueueProviders
                .SelectMany(x => x.GetJobQueueMonitoringApi().GetQueues())
                .Count();

            return statistics;
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(DbConnection connection, string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => $"stats:{type}:{x.ToString("yyyy-MM-dd-HH")}", x => x);

            return GetTimelineStats(connection, keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(DbConnection connection, string type)
        {
            var endDate = DateTime.UtcNow.Date;
            var dates = new List<DateTime>();
            for (var i = 0; i < 7; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddDays(-1);
            }

            var keyMaps = dates.ToDictionary(x => $"stats:{type}:{x.ToString("yyyy-MM-dd")}", x => x);

            return GetTimelineStats(connection, keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(
            DbConnection connection,
            IDictionary<string, DateTime> keyMaps)
        {
            string sqlQuery =
$@"select [Key], [Value] as [Count] from [{_storage.SchemaName}].AggregatedCounter with (nolock, forceseek)
where [Key] in @keys";

            var valuesMap = connection.Query(
                sqlQuery,
                new { keys = keyMaps.Keys },
                commandTimeout: _storage.CommandTimeout)
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

        private T UseConnection<T>(Func<DbConnection, T> action)
        {
            return _storage.UseConnection(null, action);
        }

        private JobList<EnqueuedJobDto> EnqueuedJobs(DbConnection connection, long[] jobIds)
        {
            string enqueuedJobsSql = 
$@"select j.*, s.Reason as StateReason, s.Data as StateData, s.CreatedAt as StateChanged
from [{_storage.SchemaName}].Job j with (nolock, forceseek)
left join [{_storage.SchemaName}].State s with (nolock, forceseek) on s.Id = j.StateId and s.JobId = j.Id
where j.Id in @jobIds";

            var jobs = connection.Query<SqlJob>(
                enqueuedJobsSql,
                new { jobIds = jobIds },
                commandTimeout: _storage.CommandTimeout)
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
                    InEnqueuedState = EnqueuedState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    EnqueuedAt = EnqueuedState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase)
                        ? sqlJob.StateChanged
                        : null
                });
        }

        private long GetNumberOfJobsByStateName(DbConnection connection, string stateName)
        {
            var sqlQuery = _jobListLimit.HasValue
                ? $@"select count(j.Id) from (select top (@limit) Id from [{_storage.SchemaName}].Job with (nolock, forceseek) where StateName = @state) as j"
                : $@"select count(Id) from [{_storage.SchemaName}].Job with (nolock, forceseek) where StateName = @state";

            var count = connection.ExecuteScalar<int>(
                 sqlQuery,
                 new { state = stateName, limit = _jobListLimit },
                 commandTimeout: _storage.CommandTimeout);

            return count;
        }

        private static Job DeserializeJob(string invocationData, string arguments, out InvocationData data, out Exception exception)
        {
            data = InvocationData.DeserializePayload(invocationData);

            if (!String.IsNullOrEmpty(arguments))
            {
                data.Arguments = arguments;
            }

            try
            {
                exception = null;
                return data.DeserializeJob();
            }
            catch (JobLoadException ex)
            {
                exception = ex;
                return null;
            }
        }

        private JobList<TDto> GetJobs<TDto>(
            DbConnection connection,
            int from,
            int count,
            string stateName,
            Func<SqlJob, Job, SafeDictionary<string, string>, TDto> selector)
        {
            string jobsSql = 
$@";with cte as 
(
  select j.Id, row_number() over (order by j.Id desc) as row_num
  from [{_storage.SchemaName}].Job j with (nolock, forceseek)
  where j.StateName = @stateName
)
select j.*, s.Reason as StateReason, s.Data as StateData, s.CreatedAt as StateChanged
from [{_storage.SchemaName}].Job j with (nolock, forceseek)
inner join cte on cte.Id = j.Id
left join [{_storage.SchemaName}].State s with (nolock, forceseek) on j.StateId = s.Id and j.Id = s.JobId
where cte.row_num between @start and @end";

            var jobs = connection.Query<SqlJob>(
                        jobsSql,
                        new { stateName = stateName, start = @from + 1, end = @from + count },
                        commandTimeout: _storage.CommandTimeout)
                        .ToList();

            return DeserializeJobs(jobs, selector);
        }

        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<SqlJob> jobs,
            Func<SqlJob, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);
            
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var job in jobs)
            {
                var dto = default(TDto);
                
                if (job.InvocationData != null)
                {
                    var deserializedData = SerializationHelper.Deserialize<Dictionary<string, string>>(job.StateData);
                    var stateData = deserializedData != null
                        ? new SafeDictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                        : null;

                    dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments, out _, out _), stateData);
                }

                result.Add(new KeyValuePair<string, TDto>(
                    job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
        }

        private JobList<FetchedJobDto> FetchedJobs(DbConnection connection, IEnumerable<long> jobIds)
        { 
            string fetchedJobsSql = 
$@"select j.*, s.Reason as StateReason, s.Data as StateData 
from [{_storage.SchemaName}].Job j with (nolock, forceseek)
left join [{_storage.SchemaName}].State s with (nolock, forceseek) on s.Id = j.StateId and s.JobId = j.Id
where j.Id in @jobIds";

            var jobs = connection.Query<SqlJob>(
                fetchedJobsSql,
                new { jobIds = jobIds },
                commandTimeout: _storage.CommandTimeout)
                .ToList();

            var result = new List<KeyValuePair<string, FetchedJobDto>>(jobs.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var job in jobs)
            {
                result.Add(new KeyValuePair<string, FetchedJobDto>(
                    job.Id.ToString(),
                    new FetchedJobDto
                    {
                        Job = DeserializeJob(job.InvocationData, job.Arguments, out _, out _),
                        State = job.StateName,
                    }));
            }

            return new JobList<FetchedJobDto>(result);
        }

        /// <summary>
        /// Overloaded dictionary that doesn't throw if given an invalid key
        /// Fixes issues such as https://github.com/HangfireIO/Hangfire/issues/871
        /// </summary>
        private class SafeDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            public SafeDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) 
                : base(dictionary, comparer)
            {
            }

            public new TValue this[TKey i]
            {
                get { return ContainsKey(i) ? base[i] : default(TValue); }
                set { base[i] = value; }
            }
        }
    }
}

