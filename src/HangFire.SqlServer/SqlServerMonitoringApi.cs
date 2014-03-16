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
        private readonly SqlConnection _connection;
        private readonly TransactionScope _transaction;

        public SqlServerMonitoringApi(SqlConnection connection)
        {
            _connection = connection;
            _transaction = new TransactionScope(
                TransactionScopeOption.RequiresNew,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted });

            _connection.EnlistTransaction(Transaction.Current);
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
                + @"where Queue = @queue and FetchedAt is NULL",
                new { queue = queue })
                .Single();
        }

        public long DequeuedCount(string queue)
        {
            return _connection.Query<int>(
                @"select count(JobId) from HangFire.JobQueue "
                + @"where Queue = @queue and FetchedAt is not NULL",
                new { queue = queue })
                .Single();
        }

        public long FailedCount()
        {
            return GetNumberOfJobsByStateName(FailedState.Name);
        }

        private long GetNumberOfJobsByStateName(string stateName)
        {
            const string sqlQuery = @"
select count(Id) from HangFire.Job where StateName = @state";

            return _connection.Query<int>(
                sqlQuery,
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
select * from (
  select j.*, s.Reason as StateReason, s.Data as StateData, row_number() over (order by j.Id desc) as row_num
  from HangFire.Job j
  left join HangFire.State s on j.StateId = s.Id
  where j.StateName = @stateName
) as j where j.row_num between @start and @end
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

            try
            {
                return JobMethod.Deserialize(data);
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
                    StartedAt = data.StartedAt.HasValue ? data.StartedAt.Value : DateTime.MinValue,
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
            public string Queue { get; set; }
            public int Enqueued { get; set; }
            public int Fetched { get; set; }
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            const string queuesAndStatusSql = @"
select distinct [Queue],
	(select count(JobId) from HangFire.JobQueue as a where q.Queue = a.Queue and a.FetchedAt is null) as Enqueued,
	(select count(JobId) from HangFire.JobQueue as b where q.Queue = b.Queue and b.FetchedAt is not null) as Fetched
from HangFire.[JobQueue] as q
";

            var queues = _connection.Query<QueueStatusDto>(queuesAndStatusSql).ToList();
            var result = new List<QueueWithTopEnqueuedJobsDto>(queues.Count);

            foreach (var queue in queues)
            {
                result.Add(new QueueWithTopEnqueuedJobsDto
                {
                    Name = queue.Queue,
                    Length = queue.Enqueued,
                    Dequeued = queue.Fetched,
                    FirstJobs = EnqueuedJobs(queue.Queue, 0, 5)
                });
            }

            return result;
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int @from, int perPage)
        {
            const string enqueuedJobsSql = @"
select * from (
  select j.*, s.Reason as StateReason, s.Data as StateData, row_number() over (order by j.Id) as row_num 
  from HangFire.JobQueue jq
  left join HangFire.Job j on jq.JobId = j.Id
  left join HangFire.State s on s.Id = j.StateId
  where jq.Queue = @queue and jq.FetchedAt is null
) as r
where r.row_num between @start and @end";

            var jobs = _connection.Query<Job>(
                enqueuedJobsSql,
                new { queue = queue, start = from + 1, end = @from + perPage })
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
select * from (
  select j.*, jq.FetchedAt, row_number() over (order by j.Id) as row_num 
  from HangFire.JobQueue jq
  left join HangFire.Job j on jq.JobId = j.Id
  where jq.Queue = @queue and jq.FetchedAt is not null
) as r
where r.row_num between @start and @end";

            var jobs = _connection.Query<Job>(
                fetchedJobsSql,
                new { queue = queue, start = from + 1, end = @from + perPage })
                .ToList();

            var result = new List<KeyValuePair<string, DequeuedJobDto>>(jobs.Count);

            foreach (var job in jobs)
            {
                result.Add(new KeyValuePair<string, DequeuedJobDto>(
                    job.Id.ToString(),
                    new DequeuedJobDto
                    {
                        Method = DeserializeJobMethod(job.InvocationData),
                        State = job.StateName,
                        CreatedAt = job.CreatedAt,
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

        public JobDetailsDto JobDetails(string jobId)
        {
            const string sql = @"
select * from HangFire.Job where Id = @id
select * from HangFire.JobParameter where JobId = @id
select * from HangFire.State where JobId = @id order by Id desc";

            using (var multi = _connection.QueryMultiple(sql, new { id = jobId }))
            {
                var job = multi.Read<Job>().SingleOrDefault();
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

                return new JobDetailsDto
                {
                    Arguments = JobHelper.FromJson<string[]>(job.Arguments),
                    CreatedAt = job.CreatedAt,
                    Method = DeserializeJobMethod(job.InvocationData),
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
select StateName as [State], count(id) as [Count] From HangFire.Job 
group by StateName
having StateName is not null;
select count(Id) from HangFire.Server;
select count(distinct Queue) from HangFire.JobQueue;
select sum([Value]) from HangFire.Counter where [Key] = 'stats:succeeded';
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

                stats.Succeeded = multi.Read<int?>().SingleOrDefault() ?? 0;
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

            const string sqlQuery = @"
select [Key], count([Value]) as Count from [HangFire].[Counter]
group by [Key]
having [Key] in @keys";

            var valuesMap = _connection.Query(
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

            const string sqlQuery = @"
select [Key], count([Value]) as Count from [HangFire].[Counter]
group by [Key]
having [Key] in @keys";

            var valuesMap = _connection.Query(
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

            return result;
        }
    }
}
