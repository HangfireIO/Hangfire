using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Dapper;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.SqlServer.Entities;
using HangFire.States;
using HangFire.Storage.Monitoring;

namespace HangFire.SqlServer
{
    public class SqlServerMonitoringApi : IMonitoringApi
    {
        private readonly SqlConnection _connection;
        private readonly TransactionScope _transaction;

        public SqlServerMonitoringApi(SqlConnection connection)
        {
            _connection = connection;
            _transaction = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted });
        }

        public void Dispose()
        {
            _transaction.Complete();
            _transaction.Dispose();
            _connection.Dispose();
        }

        public long ScheduledCount()
        {
            return GetNumberOfJobsByStateName(ScheduledState.Name);
        }

        public long EnqueuedCount(string queue)
        {
            return _connection.Query<int>(
                @"select count(JobId) from HangFire.JobQueue " 
                + @"where QueueName = @queueName and FetchedAt is NULL",
                new { queueName = queue })
                .Single();
        }

        public long DequeuedCount(string queue)
        {
            return _connection.Query<int>(
                @"select count(JobId) from HangFire.JobQueue "
                + @"where QueueName = @queueName and FetchedAt is not NULL",
                new { queueName = queue })
                .Single();
        }

        public long FailedCount()
        {
            return GetNumberOfJobsByStateName(FailedState.Name);
        }

        private long GetNumberOfJobsByStateName(string stateName)
        {
            return _connection.Query<int>(
                @"select count(Id) from HangFire.Job where State = @state",
                new { state = stateName })
                .Single();
        }

        public long ProcessingCount()
        {
            return GetNumberOfJobsByStateName(ProcessingState.Name);
        }

        public JobList<ProcessingJobDto> ProcessingJobs(int @from, int count)
        {
            return GetJobs(
                from, count,
                ProcessingState.Name,
                (method, stateData) => new ProcessingJobDto
                {
                    Method = method,
                    ServerName = stateData["ServerName"],
                    StartedAt = JobHelper.FromStringTimestamp(stateData["StartedAt"]),
                });
        }

        private JobList<TDto> GetJobs<TDto>(
            int from,
            int count,
            string stateName,
            Func<JobMethod, Dictionary<string, string>, TDto> selector)
        {
            const string jobsSql = @"
select * from (select *, row_number() over (order by CreatedAt desc) as row_num
from HangFire.Job where State = @stateName) as j where j.row_num between @start and @end
";

            var jobs = _connection.Query<Job>(
                jobsSql,
                new { stateName = stateName, start = @from + 1, end = @from + count })
                .ToList();

            return DeserializeJobs(jobs, selector);
        }

        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<Job> jobs,
            Func<JobMethod, Dictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);

            foreach (var job in jobs)
            {
                var stateData = JobHelper.FromJson<Dictionary<string, string>>(job.StateData);
                var dto = selector(DeserializeJobMethod(job.InvocationData), stateData);

                result.Add(new KeyValuePair<string, TDto>(
                    job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
        }

        private static JobMethod DeserializeJobMethod(string invocationData)
        {
            var data = JobHelper.FromJson<InvocationData>(invocationData);
            var serializedJobMethod = new Dictionary<string, string>
                {
                    { "Type", data.Type },
                    { "Method", data.Method },
                    { "ParameterTypes", data.ParameterTypes }
                };

            try
            {
                return JobMethod.Deserialize(serializedJobMethod);
            }
            catch (JobLoadException)
            {
                return null;
            }
        }

        public JobList<ScheduleDto> ScheduledJobs(int @from, int count)
        {
            return GetJobs(
                from, count,
                ScheduledState.Name,
                (method, stateData) => new ScheduleDto
                {
                    Method = method,
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
            var servers = _connection.Query<Entities.Server>(
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
                    StartedAt = DateTime.MinValue,
                    WorkersCount = data.WorkerCount
                });
            }

            return result;
        }

        public JobList<FailedJobDto> FailedJobs(int @from, int count)
        {
            return GetJobs(
                from,
                count,
                FailedState.Name,
                (method, stateData) => new FailedJobDto
                {
                    Method = method,
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
                SucceededState.Name,
                (method, stateData) => new SucceededJobDto
                {
                    Method = method,
                    SucceededAt = JobHelper.FromNullableStringTimestamp(stateData["SucceededAt"])
                });
        }

        class QueueStatusDto
        {
            public string Name { get; set; }
            public int Enqueued { get; set; }
            public int Fetched { get; set; }
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            const string queuesAndStatusSql = @"
select 
	[Name],
	(select count(JobId) from HangFire.JobQueue as a where q.Name = a.QueueName and a.FetchedAt is null) as Enqueued,
	(select count(JobId) from HangFire.JobQueue as b where q.Name = b.QueueName and b.FetchedAt is not null) as Fetched
from HangFire.Queue as q";

            var queues = _connection.Query<QueueStatusDto>(queuesAndStatusSql).ToList();
            var result = new List<QueueWithTopEnqueuedJobsDto>(queues.Count);

            foreach (var queue in queues)
            {
                result.Add(new QueueWithTopEnqueuedJobsDto
                {
                    Name = queue.Name,
                    Length = queue.Enqueued,
                    Dequeued = queue.Fetched,
                    FirstJobs = new List<KeyValuePair<string, EnqueuedJobDto>>() // TODO: implement
                });
            }

            return result;
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int @from, int perPage)
        {
            const string enqueuedJobsSql = @"
select * from
(select j.*, row_number() over (order by j.CreatedAt) as row_num from HangFire.JobQueue jq
left join HangFire.Job j on jq.JobId = j.Id
where jq.QueueName = @queueName and jq.FetchedAt is null) as r
where r.row_num between @start and @end";

            var jobs = _connection.Query<Job>(
                enqueuedJobsSql,
                new { queueName = queue, start = from + 1, end = @from + perPage })
                .ToList();

            return DeserializeJobs(
                jobs,
                (method, stateData) => new EnqueuedJobDto
                {
                    Method = method,
                    EnqueuedAt = JobHelper.FromNullableStringTimestamp(stateData["EnqueuedAt"])
                });
        }

        public JobList<DequeuedJobDto> DequeuedJobs(string queue, int @from, int perPage)
        {
            const string fetchedJobsSql = @"
select * from
(select j.*, jq.FetchedAt, jq.CheckedAt, row_number() over (order by j.CreatedAt) as row_num from HangFire.JobQueue jq
left join HangFire.Job j on jq.JobId = j.Id
where jq.QueueName = @queueName and jq.FetchedAt is not null) as r
where r.row_num between @start and @end";

            var jobs = _connection.Query<Job>(
                fetchedJobsSql,
                new { queueName = queue, start = from + 1, end = @from + perPage })
                .ToList();

            var result = new List<KeyValuePair<string, DequeuedJobDto>>(jobs.Count);

            foreach (var job in jobs)
            {
                result.Add(new KeyValuePair<string, DequeuedJobDto>(
                    job.Id.ToString(),
                    new DequeuedJobDto
                    {
                        Method = DeserializeJobMethod(job.InvocationData),
                        State = job.State,
                        CreatedAt = job.CreatedAt,
                        CheckedAt = job.CheckedAt,
                        FetchedAt = job.FetchedAt
                    }));
            }

            return new JobList<DequeuedJobDto>(result);
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return GetHourlyTimelineStats("succeeded");
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return GetHourlyTimelineStats("failed");
        }

        public bool RetryJob(string jobId)
        {
            throw new NotImplementedException();
        }

        public bool EnqueueScheduled(string jobId)
        {
            throw new NotImplementedException();
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            const string sql = @"
select * from HangFire.Job where Id = @id
select * from HangFire.JobParameter where JobId = @id
select * from HangFire.JobHistory where JobId = @id order by CreatedAt desc";

            using (var multi = _connection.QueryMultiple(sql, new { id = jobId }))
            {
                var job = multi.Read<Job>().SingleOrDefault();
                if (job == null) return null;

                var parameters = multi.Read<JobParameter>().ToDictionary(x => x.Name, x => x.Value);
                var history =
                    multi.Read<JobHistory>()
                        .ToList()
                        .Select(x => JobHelper.FromJson<Dictionary<string, string>>(x.Data))
                        .ToList();

                var invocationData = JobHelper.FromJson<InvocationData>(job.InvocationData);
                var invocationDictionary = new Dictionary<string, string>
                {
                    { "Type", invocationData.Type },
                    { "Method", invocationData.Method },
                    { "ParameterTypes", invocationData.ParameterTypes }
                };

                return new JobDetailsDto
                {
                    Arguments = JobHelper.FromJson<string[]>(job.Arguments),
                    CreatedAt = job.CreatedAt,
                    State = job.State,
                    Method = JobMethod.Deserialize(invocationDictionary),
                    History = history,
                    Properties = parameters
                };
            }
        }

        public long SucceededListCount()
        {
            return GetNumberOfJobsByStateName(SucceededState.Name);
        }

        public StatisticsDto GetStatistics()
        {
            var stats = new StatisticsDto();

            const string sql = @"
select [State], count(id) as [Count] From HangFire.Job group by [State]
select count(Id) from HangFire.Server
select count(Name) from HangFire.Queue
select IntValue from HangFire.Value where [Key] = 'stats:succeeded'
";

            using (var multi = _connection.QueryMultiple(sql))
            {
                var countByStates = multi.Read().ToDictionary(x => x.State, x => x.Count);

                Func<string, int> getCountIfExists = name => countByStates.ContainsKey(name) ? countByStates[name] : 0;

                stats.Enqueued = getCountIfExists(EnqueuedState.Name);
                stats.Failed = getCountIfExists(FailedState.Name);
                stats.Processing = getCountIfExists(ProcessingState.Name);
                stats.Scheduled = getCountIfExists(ScheduledState.Name);
                
                stats.Servers = multi.Read<int>().Single();
                stats.Queues = multi.Read<int>().Single();

                stats.Succeeded = multi.Read<int>().SingleOrDefault();
            }

            return stats;
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keys = dates.Select(x => String.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd-HH"))).ToList();
            var valuesMap = _connection.Query(
                @"select [Key], IntValue from HangFire.Value where [Key] in @keys",
                new { keys = keys })
                .ToDictionary(x => (string)x.Key, x => (long)x.IntValue);

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

            return result;
        }

        private Dictionary<DateTime, long> GetTimelineStats(string type)
        {
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

            var valuesMap = _connection.Query(
                @"select [Key], IntValue from HangFire.Value where [Key] in @keys",
                new { keys = keys })
                .ToDictionary(x => (string)x.Key, x => (long)x.IntValue);

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

            return result;
        }
    }
}
