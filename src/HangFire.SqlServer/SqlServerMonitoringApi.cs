// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Dapper;
using HangFire.Common;
using HangFire.SqlServer.Entities;
using HangFire.States;
using HangFire.Storage;
using HangFire.Storage.Monitoring;

namespace HangFire.SqlServer
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
            return GetNumberOfJobsByStateName(ScheduledState.StateName);
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
            return GetNumberOfJobsByStateName(FailedState.StateName);
        }

        private long GetNumberOfJobsByStateName(string stateName)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);

                    const string sqlQuery = @"
select count(Id) from HangFire.Job where StateName = @state";

                    var count = connection.Query<int>(
                        sqlQuery,
                        new { state = stateName })
                        .Single();

                    transaction.Complete();

                    return count;
                }
            }
        }

        public long ProcessingCount()
        {
            return GetNumberOfJobsByStateName(ProcessingState.StateName);
        }

        public JobList<ProcessingJobDto> ProcessingJobs(int @from, int count)
        {
            return GetJobs(
                from, count,
                ProcessingState.StateName,
                (sqlJob, job, stateData) => new ProcessingJobDto
                {
                    Job = job,
                    ServerId = stateData.ContainsKey("ServerId") ? stateData["ServerId"] : stateData["ServerName"],
                    StartedAt = JobHelper.FromStringTimestamp(stateData["StartedAt"]),
                });
        }

        private JobList<TDto> GetJobs<TDto>(
            int from,
            int count,
            string stateName,
            Func<SqlJob, Job, Dictionary<string, string>, TDto> selector)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);
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

                    transaction.Complete();

                    return DeserializeJobs(jobs, selector);
                }
            }
        }

        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<SqlJob> jobs,
            Func<SqlJob, Job, Dictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);

            foreach (var job in jobs)
            {
                var stateData = JobHelper.FromJson<Dictionary<string, string>>(job.StateData);
                var dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments), stateData);

                result.Add(new KeyValuePair<string, TDto>(
                    job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
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

        public JobList<ScheduledJobDto> ScheduledJobs(int @from, int count)
        {
            return GetJobs(
                from, count,
                ScheduledState.StateName,
                (sqlJob, job, stateData) => new ScheduledJobDto
                {
                    Job = job,
                    EnqueueAt = JobHelper.FromStringTimestamp(stateData["EnqueueAt"]),
                    ScheduledAt = JobHelper.FromStringTimestamp(stateData["ScheduledAt"])
                });
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return GetTimelineStats("succeeded");
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            return GetTimelineStats("failed");
        }

        public IList<ServerDto> Servers()
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);

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

                    transaction.Complete();

                    return result;
                }
            }
        }

        public JobList<FailedJobDto> FailedJobs(int @from, int count)
        {
            return GetJobs(
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
                    FailedAt = JobHelper.FromNullableStringTimestamp(stateData["FailedAt"])
                });
        }

        public JobList<SucceededJobDto> SucceededJobs(int @from, int count)
        {
            return GetJobs(
                from,
                count,
                SucceededState.StateName,
                (sqlJob, job, stateData) => new SucceededJobDto
                {
                    Job = job,
                    TotalDuration = stateData.ContainsKey("PerformanceDuration") && stateData.ContainsKey("Latency")
                        ? (long?)long.Parse(stateData["PerformanceDuration"]) + (long?)long.Parse(stateData["Latency"])
                        : null,
                    SucceededAt = JobHelper.FromNullableStringTimestamp(stateData["SucceededAt"])
                });
        }

        public JobList<DeletedJobDto> DeletedJobs(int @from, int count)
        {
            return GetJobs(
                from,
                count,
                DeletedState.StateName,
                (sqlJob, job, stateData) => new DeletedJobDto
                {
                    Job = job,
                    DeletedAt = JobHelper.FromNullableStringTimestamp(stateData["DeletedAt"])
                });
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);

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
                            FirstJobs = EnqueuedJobs(enqueuedJobIds)
                        });
                    }

                    transaction.Complete();

                    return result;
                }
            }
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int @from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var enqueuedJobIds = queueApi.GetEnqueuedJobIds(queue, from, perPage);

            return EnqueuedJobs(enqueuedJobIds);
        }

        private JobList<EnqueuedJobDto> EnqueuedJobs(IEnumerable<int> jobIds)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);

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

                    transaction.Complete();

                    return DeserializeJobs(
                        jobs,
                        (sqlJob, job, stateData) => new EnqueuedJobDto
                        {
                            Job = job,
                            State = sqlJob.StateName,
                            EnqueuedAt = sqlJob.StateName == EnqueuedState.StateName
                                ? JobHelper.FromNullableStringTimestamp(stateData["EnqueuedAt"])
                                : null
                        });
                }
            }
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var fetchedJobIds = queueApi.GetFetchedJobIds(queue, from, perPage);

            return FetchedJobs(fetchedJobIds);
        }

        private JobList<FetchedJobDto> FetchedJobs(IEnumerable<int> jobIds)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);

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

                    transaction.Complete();

                    return new JobList<FetchedJobDto>(result);
                }
            }
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return GetHourlyTimelineStats("succeeded");
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return GetHourlyTimelineStats("failed");
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);

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
                                     Data = JobHelper.FromJson<Dictionary<string, string>>(x.Data)
                                 })
                                .ToList();

                        transaction.Complete();

                        return new JobDetailsDto
                        {
                            CreatedAt = job.CreatedAt,
                            Job = DeserializeJob(job.InvocationData, job.Arguments),
                            History = history,
                            Properties = parameters
                        };
                    }
                }
            }
        }

        public long SucceededListCount()
        {
            return GetNumberOfJobsByStateName(SucceededState.StateName);
        }

        public long DeletedListCount()
        {
            return GetNumberOfJobsByStateName(DeletedState.StateName);
        }

        public StatisticsDto GetStatistics()
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);

                    var stats = new StatisticsDto();

                    const string sql = @"
select StateName as [State], count(id) as [Count] From HangFire.Job 
group by StateName
having StateName is not null;
select count(Id) from HangFire.Server;
select sum([Value]) from HangFire.Counter where [Key] = N'stats:succeeded';
select sum([Value]) from HangFire.Counter where [Key] = N'stats:deleted';
";

                    using (var multi = connection.QueryMultiple(sql))
                    {
                        var countByStates = multi.Read().ToDictionary(x => x.State, x => x.Count);

                        Func<string, int> getCountIfExists = name => countByStates.ContainsKey(name) ? countByStates[name] : 0;

                        stats.Enqueued = getCountIfExists(EnqueuedState.StateName);
                        stats.Failed = getCountIfExists(FailedState.StateName);
                        stats.Processing = getCountIfExists(ProcessingState.StateName);
                        stats.Scheduled = getCountIfExists(ScheduledState.StateName);

                        stats.Servers = multi.Read<int>().Single();

                        stats.Succeeded = multi.Read<int?>().SingleOrDefault() ?? 0;
                        stats.Deleted = multi.Read<int?>().SingleOrDefault() ?? 0;
                    }

                    stats.Queues = _queueProviders
                        .SelectMany(x => x.GetJobQueueMonitoringApi(connection).GetQueues())
                        .Count();

                    transaction.Complete();

                    return stats;
                }
            }
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(string type)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);

                    var endDate = DateTime.UtcNow;
                    var dates = new List<DateTime>();
                    for (var i = 0; i < 24; i++)
                    {
                        dates.Add(endDate);
                        endDate = endDate.AddHours(-1);
                    }

                    var keys = dates.Select(x => String.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd-HH"))).ToList();

                    const string sqlQuery = @"
select [Key], count([Value]) as Count from [HangFire].[Counter]
group by [Key]
having [Key] in @keys";

                    var valuesMap = connection.Query(
                        sqlQuery,
                        new { keys = keys })
                        .ToDictionary(x => (string)x.Key, x => (long)x.Count);

                    foreach (var key in keys)
                    {
                        if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);
                    }

                    var result = new Dictionary<DateTime, long>();
                    for (var i = 0; i < dates.Count; i++)
                    {
                        var value = valuesMap[valuesMap.Keys.ElementAt(i)];
                        result.Add(dates[i], value);
                    }

                    transaction.Complete();

                    return result;
                }
            }
        }

        private Dictionary<DateTime, long> GetTimelineStats(string type)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);

                    var endDate = DateTime.UtcNow.Date;
                    var startDate = endDate.AddDays(-7);
                    var dates = new List<DateTime>();

                    while (startDate <= endDate)
                    {
                        dates.Add(endDate);
                        endDate = endDate.AddDays(-1);
                    }

                    var stringDates = dates.Select(x => x.ToString("yyyy-MM-dd")).ToList();
                    var keys = stringDates.Select(x => String.Format("stats:{0}:{1}", type, x)).ToList();

                    const string sqlQuery = @"
select [Key], count([Value]) as Count from [HangFire].[Counter]
group by [Key]
having [Key] in @keys";

                    var valuesMap = connection.Query(
                        sqlQuery,
                        new { keys = keys })
                        .ToDictionary(x => (string)x.Key, x => (long)x.Count);

                    foreach (var key in keys)
                    {
                        if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);
                    }

                    var result = new Dictionary<DateTime, long>();
                    for (var i = 0; i < stringDates.Count; i++)
                    {
                        var value = valuesMap[valuesMap.Keys.ElementAt(i)];
                        result.Add(dates[i], value);
                    }

                    transaction.Complete();

                    return result;
                }
            }
        }

        private IPersistentJobQueueMonitoringApi GetQueueApi(string queueName)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.EnlistTransaction(Transaction.Current);

                    var provider = _queueProviders.GetProvider(queueName);
                    var monitoringApi = provider.GetJobQueueMonitoringApi(connection);

                    transaction.Complete();

                    return monitoringApi;
                }
            }
        }
    }
}
