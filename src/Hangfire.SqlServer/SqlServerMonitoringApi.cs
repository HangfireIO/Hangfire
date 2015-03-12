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
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Transactions;
using Dapper;
using Hangfire.Common;
using Hangfire.SqlServer.Entities;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.SqlServer
{
    internal class SqlServerMonitoringApi : IMonitoringApi
    {
        private readonly string _connectionString;
        private readonly PersistentJobQueueProviderCollection _queueProviders;

        public SqlServerMonitoringApi(
            string connectionString,
            PersistentJobQueueProviderCollection queueProviders)
        {
            _connectionString = connectionString;
            _queueProviders = queueProviders;
        }

        public long ScheduledCount()
        {
            return UseConnection(connection => 
                GetNumberOfJobsByStateName(connection, ScheduledState.StateName));
        }

        public long EnqueuedCount(string queue)
        {
            return UseConnection(connection =>
            {
                var queueApi = GetQueueApi(connection, queue);
                var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

                return counters.EnqueuedCount ?? 0;
            });
        }

        public long FetchedCount(string queue)
        {
            return UseConnection(connection =>
            {
                var queueApi = GetQueueApi(connection, queue);
                var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

                return counters.FetchedCount ?? 0;
            });
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
                    ServerId = stateData.ContainsKey("ServerId") ? stateData["ServerId"] : stateData["ServerName"],
                    StartedAt = JobHelper.DeserializeDateTime(stateData["StartedAt"]),
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
                    EnqueueAt = JobHelper.DeserializeDateTime(stateData["EnqueueAt"]),
                    ScheduledAt = JobHelper.DeserializeDateTime(stateData["ScheduledAt"])
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
                    @"select * from HangFire.Server")
                    .ToList();

                var result = new List<ServerDto>();

                foreach (var server in servers)
                {
                    var data = JobHelper.FromJson<ServerData>(server.Data);
                    result.Add(new ServerDto
                    {
                        Name = server.Id,
                        Heartbeat = server.LastHeartbeat,
                        Queues = data.Queues,
                        StartedAt = data.StartedAt.HasValue ? data.StartedAt.Value : DateTime.MinValue,
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
                    Reason = sqlJob.StateReason,
                    ExceptionDetails = stateData["ExceptionDetails"],
                    ExceptionMessage = stateData["ExceptionMessage"],
                    ExceptionType = stateData["ExceptionType"],
                    FailedAt = JobHelper.DeserializeNullableDateTime(stateData["FailedAt"])
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
                    Result = stateData.ContainsKey("Result") ? stateData["Result"] : null,
                    TotalDuration = stateData.ContainsKey("PerformanceDuration") && stateData.ContainsKey("Latency")
                        ? (double?)double.Parse(stateData["PerformanceDuration"], CultureInfo.InvariantCulture) + (double?)double.Parse(stateData["Latency"], CultureInfo.InvariantCulture)
                        : null,
                    SucceededAt = JobHelper.DeserializeNullableDateTime(stateData["SucceededAt"])
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
                    DeletedAt = JobHelper.DeserializeNullableDateTime(stateData["DeletedAt"])
                }));
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            return UseConnection<IList<QueueWithTopEnqueuedJobsDto>>(connection =>
            {
                var tuples = _queueProviders
                    .Select(x => x.GetJobQueueMonitoringApi(connection))
                    .SelectMany(x => x.GetQueues(), (monitoring, queue) => new { Monitoring = monitoring, Queue = queue })
                    .OrderBy(x => x.Queue)
                    .ToArray();

                var result = new List<QueueWithTopEnqueuedJobsDto>(tuples.Length);

                foreach (var tuple in tuples)
                {
                    var enqueuedJobIds = tuple.Monitoring.GetEnqueuedJobIds(tuple.Queue, 0, 5);
                    var counters = tuple.Monitoring.GetEnqueuedAndFetchedCount(tuple.Queue);

                    result.Add(new QueueWithTopEnqueuedJobsDto
                    {
                        Name = tuple.Queue,
                        Length = counters.EnqueuedCount ?? 0,
                        Fetched = counters.FetchedCount,
                        FirstJobs = EnqueuedJobs(connection, enqueuedJobIds)
                    });
                }

                return result;
            });
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int @from, int perPage)
        {
            return UseConnection(connection =>
            {
                var queueApi = GetQueueApi(connection, queue);
                var enqueuedJobIds = queueApi.GetEnqueuedJobIds(queue, from, perPage);

                return EnqueuedJobs(connection, enqueuedJobIds);
            });
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            return UseConnection(connection =>
            {
                var queueApi = GetQueueApi(connection, queue);
                var fetchedJobIds = queueApi.GetFetchedJobIds(queue, from, perPage);

                return FetchedJobs(connection, fetchedJobIds);
            });
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

                const string sql = @"
select * from HangFire.Job where Id = @id
select * from HangFire.JobParameter where JobId = @id
select * from HangFire.State where JobId = @id order by Id desc";

                using (var multi = connection.QueryMultiple(sql, new { id = jobId }))
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
            return UseConnection(connection =>
            {
                const string sql = @"
select StateName as [State], count(Id) as [Count] From HangFire.Job 
group by StateName
having StateName is not null;
select count(Id) from HangFire.Server;
select sum(s.[Value]) from (
    select sum([Value]) as [Value] from HangFire.Counter where [Key] = N'stats:succeeded'
    union all
    select [Value] from HangFire.AggregatedCounter where [Key] = N'stats:succeeded'
) as s;
select sum(s.[Value]) from (
    select sum([Value]) as [Value] from HangFire.Counter where [Key] = N'stats:deleted'
    union all
    select [Value] from HangFire.AggregatedCounter where [Key] = N'stats:deleted'
) as s;
select count(*) from HangFire.[Set] where [Key] = N'recurring-jobs';
";

                var stats = new StatisticsDto();
                using (var multi = connection.QueryMultiple(sql))
                {
                    var countByStates = multi.Read().ToDictionary(x => x.State, x => x.Count);

                    Func<string, int> getCountIfExists = name => countByStates.ContainsKey(name) ? countByStates[name] : 0;

                    stats.Enqueued = getCountIfExists(EnqueuedState.StateName);
                    stats.Failed = getCountIfExists(FailedState.StateName);
                    stats.Processing = getCountIfExists(ProcessingState.StateName);
                    stats.Scheduled = getCountIfExists(ScheduledState.StateName);

                    stats.Servers = multi.Read<int>().Single();

                    stats.Succeeded = multi.Read<long?>().SingleOrDefault() ?? 0;
                    stats.Deleted = multi.Read<long?>().SingleOrDefault() ?? 0;

                    stats.Recurring = multi.Read<int>().Single();
                }

                stats.Queues = _queueProviders
                    .SelectMany(x => x.GetJobQueueMonitoringApi(connection).GetQueues())
                    .Count();

                return stats;
            });
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(
            SqlConnection connection,
            string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => String.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd-HH")), x => x);

            return GetTimelineStats(connection, keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(
            SqlConnection connection,
            string type)
        {
            var endDate = DateTime.UtcNow.Date;
            var dates = new List<DateTime>();
            for (var i = 0; i < 7; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddDays(-1);
            }

            var keyMaps = dates.ToDictionary(x => String.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd")), x => x);

            return GetTimelineStats(connection, keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(SqlConnection connection,
            IDictionary<string, DateTime> keyMaps)
        {
            const string sqlQuery = @"
select [Key], [Value] as Count from [HangFire].[AggregatedCounter]
where [Key] in @keys";

            var valuesMap = connection.Query(
                sqlQuery,
                new { keys = keyMaps.Keys })
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

        private IPersistentJobQueueMonitoringApi GetQueueApi(
            SqlConnection connection,
            string queueName)
        {
            var provider = _queueProviders.GetProvider(queueName);
            var monitoringApi = provider.GetJobQueueMonitoringApi(connection);

            return monitoringApi;
        }

        private T UseConnection<T>(Func<SqlConnection, T> action)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var result = action(connection);

                transaction.Complete();

                return result;
            }
        }

        private JobList<EnqueuedJobDto> EnqueuedJobs(
            SqlConnection connection,
            IEnumerable<int> jobIds)
        {
            const string enqueuedJobsSql = @"
select j.*, s.Reason as StateReason, s.Data as StateData 
from HangFire.Job j
left join HangFire.State s on s.Id = j.StateId
left join HangFire.JobQueue jq on jq.JobId = j.Id
where j.Id in @jobIds and jq.FetchedAt is null";

            var jobs = connection.Query<SqlJob>(
                enqueuedJobsSql,
                new { jobIds = jobIds })
                .ToList();

            return DeserializeJobs(
                jobs,
                (sqlJob, job, stateData) => new EnqueuedJobDto
                {
                    Job = job,
                    State = sqlJob.StateName,
                    EnqueuedAt = sqlJob.StateName == EnqueuedState.StateName
                        ? JobHelper.DeserializeNullableDateTime(stateData["EnqueuedAt"])
                        : null
                });
        }

        private long GetNumberOfJobsByStateName(SqlConnection connection, string stateName)
        {
            const string sqlQuery = @"
select count(Id) from HangFire.Job where StateName = @state";

            var count = connection.Query<int>(
                 sqlQuery,
                 new { state = stateName })
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
            SqlConnection connection,
            int from,
            int count,
            string stateName,
            Func<SqlJob, Job, Dictionary<string, string>, TDto> selector)
        {
            const string jobsSql = @"
select * from (
  select j.*, s.Reason as StateReason, s.Data as StateData, row_number() over (order by j.Id desc) as row_num
  from HangFire.Job j
  left join HangFire.State s on j.StateId = s.Id
  where j.StateName = @stateName
) as j where j.row_num between @start and @end
";

            var jobs = connection.Query<SqlJob>(
                        jobsSql,
                        new { stateName = stateName, start = @from + 1, end = @from + count })
                        .ToList();

            return DeserializeJobs(jobs, selector);
        }

        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<SqlJob> jobs,
            Func<SqlJob, Job, Dictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);

            foreach (var job in jobs)
            {
                var deserializedData = JobHelper.FromJson<Dictionary<string, string>>(job.StateData);
                var stateData = deserializedData != null
                    ? new Dictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                    : null;

                var dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments), stateData);

                result.Add(new KeyValuePair<string, TDto>(
                    job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
        }

        private JobList<FetchedJobDto> FetchedJobs(
            SqlConnection connection,
            IEnumerable<int> jobIds)
        {
            const string fetchedJobsSql = @"
select j.*, jq.FetchedAt, s.Reason as StateReason, s.Data as StateData 
from HangFire.Job j
left join HangFire.State s on s.Id = j.StateId
left join HangFire.JobQueue jq on jq.JobId = j.Id
where j.Id in @jobIds and jq.FetchedAt is not null";

            var jobs = connection.Query<SqlJob>(
                fetchedJobsSql,
                new { jobIds = jobIds })
                .ToList();

            var result = new List<KeyValuePair<string, FetchedJobDto>>(jobs.Count);

            foreach (var job in jobs)
            {
                result.Add(new KeyValuePair<string, FetchedJobDto>(
                    job.Id.ToString(),
                    new FetchedJobDto
                    {
                        Job = DeserializeJob(job.InvocationData, job.Arguments),
                        State = job.StateName,
                        FetchedAt = job.FetchedAt
                    }));
            }

            return new JobList<FetchedJobDto>(result);
        }
    }
}
