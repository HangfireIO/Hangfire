// // This file is part of Hangfire.
// // Copyright © 2013-2014 Sergey Odinokov.
// // 
// // Hangfire is free software: you can redistribute it and/or modify
// // it under the terms of the GNU Lesser General Public License as 
// // published by the Free Software Foundation, either version 3 
// // of the License, or any later version.
// // 
// // Hangfire is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// // GNU Lesser General Public License for more details.
// // 
// // You should have received a copy of the GNU Lesser General Public 
// // License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Hangfire.Common;
using Hangfire.Sql.Entities;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Sql
{
    public class SqlMonitoringApi : IMonitoringApi
    {
        private readonly IConnectionProvider _connectionProvider;
        protected readonly PersistentJobQueueProviderCollection QueueProviders;
        protected readonly SqlBook SqlBook;

        public SqlMonitoringApi(
            IConnectionProvider connectionProvider,
            SqlBook sqlBook,
            PersistentJobQueueProviderCollection queueProviders)
        {
            _connectionProvider = connectionProvider;
            SqlBook = sqlBook;
            QueueProviders = queueProviders;
        }

        public long ScheduledCount()
        {
            return UseConnection((connection, transaction) =>
                GetNumberOfJobsByStateName(connection, transaction, ScheduledState.StateName));
        }

        public long EnqueuedCount(string queue)
        {
            return UseConnection((connection, transaction) =>
            {
                var queueApi = GetQueueApi(queue);
                var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

                return counters.EnqueuedCount ?? 0;
            });
        }

        public long FetchedCount(string queue)
        {
            return UseConnection((connection, transaction) =>
            {
                var queueApi = GetQueueApi(queue);
                var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

                return counters.FetchedCount ?? 0;
            });
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
                    SqlBook.SqlMonitoringApi_Servers, transaction: transaction)
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
                        ? (long?) long.Parse(stateData["PerformanceDuration"]) +
                          (long?) long.Parse(stateData["Latency"])
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
            return UseConnection<IList<QueueWithTopEnqueuedJobsDto>>((connection, transaction) =>
            {
                var tuples = QueueProviders
                    .Select(x => x.GetJobQueueMonitoringApi())
                    .SelectMany(x => x.GetQueues(), (monitoring, queue) => new {Monitoring = monitoring, Queue = queue})
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
                        FirstJobs = EnqueuedJobs(connection, transaction, enqueuedJobIds)
                    });
                }

                return result;
            });
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int @from, int perPage)
        {
            return UseConnection((connection, transaction) =>
            {
                var queueApi = GetQueueApi(queue);
                var enqueuedJobIds = queueApi.GetEnqueuedJobIds(queue, from, perPage);

                return EnqueuedJobs(connection, transaction, enqueuedJobIds);
            });
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            return UseConnection((connection, transaction) =>
            {
                var queueApi = GetQueueApi(queue);
                var fetchedJobIds = queueApi.GetFetchedJobIds(queue, from, perPage);

                return FetchedJobs(connection, transaction, fetchedJobIds);
            });
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

        public virtual JobDetailsDto JobDetails(string jobId)
        {
            return UseConnection((connection, transaction) =>
            {
                using (var multi = connection.QueryMultiple(
                    SqlBook.SqlMonitoringApi_JobDetails,
                    new {id = jobId},
                    transaction: transaction))
                {
                    var job = multi.Read<SqlJob>().SingleOrDefault();
                    if (job == null)
                    {
                        return null;
                    }

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
                        CreatedAt = job.CreatedAt,
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

        public virtual StatisticsDto GetStatistics()
        {
            return UseConnection((connection, transaction) =>
            {
                var stats = new StatisticsDto();
                using (var multi = connection.QueryMultiple(
                    SqlBook.SqlMonitoringApi_GetStatistics,
                    transaction: transaction))
                {
                    var countByStates = multi.Read().ToDictionary(x => x.State, x => x.Count);

                    Func<string, int> getCountIfExists =
                        name => countByStates.ContainsKey(name) ? countByStates[name] : 0;

                    stats.Enqueued = getCountIfExists(EnqueuedState.StateName);
                    stats.Failed = getCountIfExists(FailedState.StateName);
                    stats.Processing = getCountIfExists(ProcessingState.StateName);
                    stats.Scheduled = getCountIfExists(ScheduledState.StateName);

                    stats.Servers = multi.Read<int>().Single();

                    stats.Succeeded = multi.Read<int?>().SingleOrDefault() ?? 0;
                    stats.Deleted = multi.Read<int?>().SingleOrDefault() ?? 0;

                    stats.Recurring = multi.Read<int>().Single();
                }

                stats.Queues = QueueProviders
                    .SelectMany(x => x.GetJobQueueMonitoringApi().GetQueues())
                    .Count();

                return stats;
            });
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(IDbConnection connection, IDbTransaction transaction,
            string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keys = dates.Select(x => String.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd-HH"))).ToList();
            var valuesMap = ExecuteHourlyTimelineStatsQuery(connection, transaction, keys);

            foreach (var key in keys)
            {
                if (!valuesMap.ContainsKey(key))
                {
                    valuesMap.Add(key, 0);
                }
            }

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < dates.Count; i++)
            {
                var value = valuesMap[valuesMap.Keys.ElementAt(i)];
                result.Add(dates[i], value);
            }

            return result;
        }

        protected virtual Dictionary<string, long> ExecuteHourlyTimelineStatsQuery(IDbConnection connection,
            IDbTransaction transaction, List<string> keys)
        {
            var valuesMap = connection.Query(
                SqlBook.SqlMonitoringApi_GetHourlyTimelineStats,
                new {keys = keys}, transaction: transaction)
                .ToDictionary(x => (string) x.Key, x => (long) x.Count);
            return valuesMap;
        }

        private Dictionary<DateTime, long> GetTimelineStats(
            IDbConnection connection,
            IDbTransaction transaction,
            string type)
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
            var valuesMap = ExecuteGetTimelineStatsQuery(connection, transaction, keys);

            foreach (var key in keys)
            {
                if (!valuesMap.ContainsKey(key))
                {
                    valuesMap.Add(key, 0);
                }
            }

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < stringDates.Count; i++)
            {
                var value = valuesMap[valuesMap.Keys.ElementAt(i)];
                result.Add(dates[i], value);
            }

            return result;
        }

        protected virtual Dictionary<string, long> ExecuteGetTimelineStatsQuery(IDbConnection connection,
            IDbTransaction transaction, List<string> keys)
        {
            var valuesMap = connection.Query(
                SqlBook.SqlMonitoringApi_GetTimelineStats,
                new {keys = keys}, transaction: transaction)
                .ToDictionary(x => (string) x.Key, x => (long) x.Count);
            return valuesMap;
        }

        private IPersistentJobQueueMonitoringApi GetQueueApi(string queueName)
        {
            var provider = QueueProviders.GetProvider(queueName);
            var monitoringApi = provider.GetJobQueueMonitoringApi();
            return monitoringApi;
        }

        protected T UseConnection<T>(Func<IDbConnection, IDbTransaction, T> action)
        {
            using (var connection = _connectionProvider.CreateAndOpenConnection())
            {
                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    var result = action(connection, transaction);
                    transaction.Commit();
                    return result;
                }
            }
        }

        protected JobList<EnqueuedJobDto> EnqueuedJobs(
            IDbConnection connection,
            IDbTransaction transaction,
            IEnumerable<int> jobIds)
        {
            return DeserializeJobs(
                GetEnqueuedJobs(connection, transaction, jobIds),
                (sqlJob, job, stateData) => new EnqueuedJobDto
                {
                    Job = job,
                    State = sqlJob.StateName,
                    EnqueuedAt = sqlJob.StateName == EnqueuedState.StateName
                        ? JobHelper.DeserializeNullableDateTime(stateData["EnqueuedAt"])
                        : null
                });
        }

        protected virtual List<SqlJob> GetEnqueuedJobs(IDbConnection connection, IDbTransaction transaction,
            IEnumerable<int> jobIds)
        {
            return connection.Query<SqlJob>(
                SqlBook.SqlMonitoringApi_EnqueuedJobs,
                new {jobIds = jobIds},
                transaction)
                .ToList();
        }

        private long GetNumberOfJobsByStateName(IDbConnection connection, IDbTransaction transaction, string stateName)
        {
            var count = Convert.ToInt64(connection.Query<int>(
                SqlBook.SqlMonitoringApi_GetNumberOfJobsByStateName,
                new {state = stateName},
                transaction: transaction).Single());
            return count;
        }

        protected Job DeserializeJob(string invocationData, string arguments)
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
            IDbConnection connection,
            IDbTransaction transaction,
            int from,
            int count,
            string stateName,
            Func<SqlJob, Job, Dictionary<string, string>, TDto> selector)
        {
            var jobs = connection.Query<SqlJob>(
                SqlBook.SqlMonitoringApi_GetJobs,
                new {stateName = stateName, vstart = @from + 1, vend = @from + count}, transaction: transaction)
                .ToList();
            return DeserializeJobs(jobs, selector);
        }

        protected JobList<TDto> DeserializeJobs<TDto>(
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

        private JobList<FetchedJobDto> FetchedJobs(
            IDbConnection connection,
            IDbTransaction transaction,
            IEnumerable<int> jobIds)
        {
            var jobs = connection.Query<SqlJob>(
                SqlBook.SqlMonitoringApi_FetchedJobs,
                new {jobIds = jobIds}, transaction: transaction)
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