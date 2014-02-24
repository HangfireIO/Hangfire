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
            return _connection.Query<long>(
                @"select count(JobId) from HangFire.JobQueue " 
                + @"where QueueName = @queueName and FetchedAt is NULL",
                new { queueName = queue })
                .Single();
        }

        public long DequeuedCount(string queue)
        {
            return _connection.Query<long>(
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

        public IList<KeyValuePair<string, ProcessingJobDto>> ProcessingJobs(int @from, int count)
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

        private IList<KeyValuePair<string, TDto>> GetJobs<TDto>(
            int from,
            int count,
            string stateName,
            Func<JobMethod, Dictionary<string, string>, TDto> selector)
        {
            // TODO: add pagination

            var jobs = _connection.Query<Job>(
                @"select * from (select *, row_number() over (order by CreatedAt desc) as row_num "+
                @"from HangFire.Job where State = @stateName) as j where j.row_num between @start and @end",
                new { stateName = stateName, start = @from + 1, end = @from + count })
                .ToList();
            
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);

            foreach (var job in jobs)
            {
                var data = JobHelper.FromJson<InvocationData>(job.InvocationData);
                var serializedJobMethod = new Dictionary<string, string>
                {
                    { "Type", data.Type },
                    { "Method", data.Method },
                    { "ParameterTypes", data.ParameterTypes }
                };
                var stateData = JobHelper.FromJson<Dictionary<string, string>>(job.StateData);

                // TODO: expected JobLoadException
                var dto = selector(JobMethod.Deserialize(serializedJobMethod), stateData); 
                
                result.Add(new KeyValuePair<string, TDto>(
                    job.Id.ToString(), dto));
            }
            
            return result;
        }

        public IDictionary<string, ScheduleDto> ScheduledJobs(int @from, int count)
        {
            return new Dictionary<string, ScheduleDto>();
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return new Dictionary<DateTime, long>();
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            return new Dictionary<DateTime, long>();
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

        public IList<KeyValuePair<string, FailedJobDto>> FailedJobs(int @from, int count)
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

        public IList<KeyValuePair<string, SucceededJobDto>> SucceededJobs(int @from, int count)
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

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            return new List<QueueWithTopEnqueuedJobsDto>();
        }

        public IList<KeyValuePair<string, EnqueuedJobDto>> EnqueuedJobs(string queue, int @from, int perPage)
        {
            return new List<KeyValuePair<string, EnqueuedJobDto>>();
        }

        public IList<KeyValuePair<string, DequeuedJobDto>> DequeuedJobs(string queue, int @from, int perPage)
        {
            return new List<KeyValuePair<string, DequeuedJobDto>>();
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return new Dictionary<DateTime, long>();
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return new Dictionary<DateTime, long>();
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
            return new JobDetailsDto();
        }

        public long SucceededListCount()
        {
            // TODO: add independent counter
            return GetNumberOfJobsByStateName(SucceededState.Name);
        }

        public StatisticsDto GetStatistics()
        {
            var stats = new StatisticsDto();

            const string sql = @"
select [State], count(id) as [Count] From HangFire.Job group by [State]
select count(Id) from HangFire.Server
select count(Name) from HangFire.Queue
";

            using (var multi = _connection.QueryMultiple(sql))
            {
                var countByStates = multi.Read().ToDictionary(x => x.State, x => x.Count);

                Func<string, int> getCountIfExists = name => countByStates.ContainsKey(name) ? countByStates[name] : 0;

                stats.Enqueued = getCountIfExists(EnqueuedState.Name);
                stats.Failed = getCountIfExists(FailedState.Name);
                stats.Processing = getCountIfExists(ProcessingState.Name);
                stats.Scheduled = getCountIfExists(ScheduledState.Name);
                stats.Succeeded = getCountIfExists(SucceededState.Name);

                stats.Servers = multi.Read<int>().Single();
                stats.Queues = multi.Read<int>().Single();
            }

            return stats;
        }
    }
}
