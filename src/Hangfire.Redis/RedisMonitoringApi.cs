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
using System.Linq;
using Hangfire.Common;
using Hangfire.Redis.Annotations;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using ServiceStack.Redis;

namespace Hangfire.Redis
{
    internal class RedisMonitoringApi : IMonitoringApi
    {
        private readonly PooledRedisClientManager _connectionPool;

        public RedisMonitoringApi([NotNull] PooledRedisClientManager connectionPool)
        {
            if (connectionPool == null) throw new ArgumentNullException("connectionPool");

            _connectionPool = connectionPool;
        }

        public long ScheduledCount()
        {
            return UseConnection(redis => 
                redis.GetSortedSetCount("hangfire:schedule"));
        }

        public long EnqueuedCount(string queue)
        {
            return UseConnection(redis => 
                redis.GetListCount(String.Format("hangfire:queue:{0}", queue)));
        }

        public long FetchedCount(string queue)
        {
            return UseConnection(redis => 
                redis.GetListCount(String.Format("hangfire:queue:{0}:dequeued", queue)));
        }

        public long FailedCount()
        {
            return UseConnection(redis => redis.GetSortedSetCount("hangfire:failed"));
        }

        public long ProcessingCount()
        {
            return UseConnection(redis => redis.GetSortedSetCount("hangfire:processing"));
        }

        public long DeletedListCount()
        {
            return UseConnection(redis => redis.GetListCount("hangfire:deleted"));
        }

        public JobList<ProcessingJobDto> ProcessingJobs(
            int from, int count)
        {
            return UseConnection(redis =>
            {
                var jobIds = redis.GetRangeFromSortedSet(
                    "hangfire:processing",
                    from,
                    from + count - 1);

                return new JobList<ProcessingJobDto>(GetJobsWithProperties(redis,
                    jobIds,
                    null,
                    new[] { "StartedAt", "ServerName", "ServerId", "State" },
                    (job, jobData, state) => new ProcessingJobDto
                    {
                        ServerId = state[2] ?? state[1],
                        Job = job,
                        StartedAt = JobHelper.DeserializeNullableDateTime(state[0]),
                        InProcessingState = ProcessingState.StateName.Equals(
                            state[3], StringComparison.OrdinalIgnoreCase),
                    }).OrderBy(x => x.Value.StartedAt).ToList());
            });
        }

        public JobList<ScheduledJobDto> ScheduledJobs(int from, int count)
        {
            return UseConnection(redis =>
            {
                var scheduledJobs = redis.GetRangeWithScoresFromSortedSet(
                    "hangfire:schedule",
                    from,
                    from + count - 1);

                if (scheduledJobs.Count == 0)
                {
                    return new JobList<ScheduledJobDto>(new List<KeyValuePair<string, ScheduledJobDto>>());
                }

                var jobs = new Dictionary<string, List<string>>();
                var states = new Dictionary<string, List<String>>();

                using (var pipeline = redis.CreatePipeline())
                {
                    foreach (var scheduledJob in scheduledJobs)
                    {
                        var job = scheduledJob;

                        pipeline.QueueCommand(
                            x => x.GetValuesFromHash(
                                String.Format("hangfire:job:{0}", job.Key),
                                new[] { "Type", "Method", "ParameterTypes", "Arguments" }),
                            x => jobs.Add(job.Key, x));

                        pipeline.QueueCommand(
                            x => x.GetValuesFromHash(
                                String.Format("hangfire:job:{0}:state", job.Key),
                                new[] { "State", "ScheduledAt" }),
                            x => states.Add(job.Key, x));
                    }

                    pipeline.Flush();
                }

                return new JobList<ScheduledJobDto>(scheduledJobs
                    .Select(job => new KeyValuePair<string, ScheduledJobDto>(
                        job.Key,
                        new ScheduledJobDto
                        {
                            EnqueueAt = JobHelper.FromTimestamp((long) job.Value),
                            Job = TryToGetJob(jobs[job.Key][0], jobs[job.Key][1], jobs[job.Key][2], jobs[job.Key][3]),
                            ScheduledAt =
                                states[job.Key].Count > 1
                                    ? JobHelper.DeserializeNullableDateTime(states[job.Key][1])
                                    : null,
                            InScheduledState =
                                ScheduledState.StateName.Equals(states[job.Key][0], StringComparison.OrdinalIgnoreCase)
                        }))
                    .ToList());
            });
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return UseConnection(redis => GetTimelineStats(redis, "succeeded"));
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            return UseConnection(redis => GetTimelineStats(redis, "failed"));
        }

        public IList<ServerDto> Servers()
        {
            return UseConnection(redis =>
            {
                var serverNames = redis.GetAllItemsFromSet("hangfire:servers");

                if (serverNames.Count == 0)
                {
                    return new List<ServerDto>();
                }

                var servers = new Dictionary<string, List<string>>();
                var queues = new Dictionary<string, List<string>>();

                using (var pipeline = redis.CreatePipeline())
                {
                    foreach (var serverName in serverNames)
                    {
                        var name = serverName;

                        pipeline.QueueCommand(
                            x => x.GetValuesFromHash(
                                String.Format("hangfire:server:{0}", name),
                                "WorkerCount", "StartedAt", "Heartbeat"),
                            x => servers.Add(name, x));

                        pipeline.QueueCommand(
                            x => x.GetAllItemsFromList(
                                String.Format("hangfire:server:{0}:queues", name)),
                            x => queues.Add(name, x));
                    }

                    pipeline.Flush();
                }

                return serverNames.Select(x => new ServerDto
                {
                    Name = x,
                    WorkersCount = int.Parse(servers[x][0]),
                    Queues = queues[x],
                    StartedAt = JobHelper.DeserializeDateTime(servers[x][1]),
                    Heartbeat = JobHelper.DeserializeNullableDateTime(servers[x][2])
                }).ToList();
            });
        }

        public JobList<FailedJobDto> FailedJobs(int from, int count)
        {
            return UseConnection(redis =>
            {
                var failedJobIds = redis.GetRangeFromSortedSetDesc(
                    "hangfire:failed",
                    from,
                    from + count - 1);

                return GetJobsWithProperties(
                    redis,
                    failedJobIds,
                    null,
                    new[] { "FailedAt", "ExceptionType", "ExceptionMessage", "ExceptionDetails", "State", "Reason" },
                    (job, jobData, state) => new FailedJobDto
                    {
                        Job = job,
                        Reason = state[5],
                        FailedAt = JobHelper.DeserializeNullableDateTime(state[0]),
                        ExceptionType = state[1],
                        ExceptionMessage = state[2],
                        ExceptionDetails = state[3],
                        InFailedState = FailedState.StateName.Equals(state[4], StringComparison.OrdinalIgnoreCase)
                    });
            });
        }

        public JobList<SucceededJobDto> SucceededJobs(int from, int count)
        {
            return UseConnection(redis =>
            {
                var succeededJobIds = redis.GetRangeFromList(
                    "hangfire:succeeded",
                    from,
                    from + count - 1);

                return GetJobsWithProperties(
                    redis,
                    succeededJobIds,
                    null,
                    new[] { "SucceededAt", "PerformanceDuration", "Latency", "State", "Result" },
                    (job, jobData, state) => new SucceededJobDto
                    {
                        Job = job,
                        Result = state[4],
                        SucceededAt = JobHelper.DeserializeNullableDateTime(state[0]),
                        TotalDuration = state[1] != null && state[2] != null
                            ? (long?) long.Parse(state[1]) + (long?) long.Parse(state[2])
                            : null,
                        InSucceededState = SucceededState.StateName.Equals(state[3], StringComparison.OrdinalIgnoreCase)
                    });
            });
        }

        public JobList<DeletedJobDto> DeletedJobs(int @from, int count)
        {
            return UseConnection(redis =>
            {
                var deletedJobIds = redis.GetRangeFromList(
                    "hangfire:deleted",
                    from,
                    from + count - 1);

                return GetJobsWithProperties(
                    redis,
                    deletedJobIds,
                    null,
                    new[] { "DeletedAt", "State" },
                    (job, jobData, state) => new DeletedJobDto
                    {
                        Job = job,
                        DeletedAt = JobHelper.DeserializeNullableDateTime(state[0]),
                        InDeletedState = DeletedState.StateName.Equals(state[1], StringComparison.OrdinalIgnoreCase)
                    });
            });
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            return UseConnection(redis =>
            {
                var queues = redis.GetAllItemsFromSet("hangfire:queues");
                var result = new List<QueueWithTopEnqueuedJobsDto>(queues.Count);

                foreach (var queue in queues)
                {
                    IList<string> firstJobIds = null;
                    long length = 0;
                    long fetched = 0;

                    using (var pipeline = redis.CreatePipeline())
                    {
                        pipeline.QueueCommand(
                            x => x.GetRangeFromList(
                                String.Format("hangfire:queue:{0}", queue), -5, -1),
                            x => firstJobIds = x);

                        pipeline.QueueCommand(
                            x => x.GetListCount(String.Format("hangfire:queue:{0}", queue)),
                            x => length = x);

                        pipeline.QueueCommand(
                            x => x.GetListCount(String.Format("hangfire:queue:{0}:dequeued", queue)),
                            x => fetched = x);

                        pipeline.Flush();
                    }

                    var jobs = GetJobsWithProperties(
                        redis,
                        firstJobIds,
                        new[] { "State" },
                        new[] { "EnqueuedAt", "State" },
                        (job, jobData, state) => new EnqueuedJobDto
                        {
                            Job = job,
                            State = jobData[0],
                            EnqueuedAt = JobHelper.DeserializeNullableDateTime(state[0]),
                            InEnqueuedState = jobData[0].Equals(state[1], StringComparison.OrdinalIgnoreCase)
                        });

                    result.Add(new QueueWithTopEnqueuedJobsDto
                    {
                        Name = queue,
                        FirstJobs = jobs,
                        Length = length,
                        Fetched = fetched
                    });
                }

                return result;
            });
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(
            string queue, int from, int perPage)
        {
            return UseConnection(redis =>
            {
                var jobIds = redis.GetRangeFromList(
                    String.Format("hangfire:queue:{0}", queue),
                    from,
                    from + perPage - 1);

                return GetJobsWithProperties(
                    redis,
                    jobIds,
                    new[] { "State" },
                    new[] { "EnqueuedAt", "State" },
                    (job, jobData, state) => new EnqueuedJobDto
                    {
                        Job = job,
                        State = jobData[0],
                        EnqueuedAt = JobHelper.DeserializeNullableDateTime(state[0]),
                        InEnqueuedState = jobData[0].Equals(state[1], StringComparison.OrdinalIgnoreCase)
                    });
            });
        }

        public JobList<FetchedJobDto> FetchedJobs(
            string queue, int from, int perPage)
        {
            return UseConnection(redis =>
            {
                var jobIds = redis.GetRangeFromList(
                    String.Format("hangfire:queue:{0}:dequeued", queue),
                    from, from + perPage - 1);

                return GetJobsWithProperties(
                    redis,
                    jobIds,
                    new[] { "State", "Fetched" },
                    null,
                    (job, jobData, state) => new FetchedJobDto
                    {
                        Job = job,
                        State = jobData[0],
                        FetchedAt = JobHelper.DeserializeNullableDateTime(jobData[1])
                    });
            });
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return UseConnection(redis => GetHourlyTimelineStats(redis, "succeeded"));
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return UseConnection(redis => GetHourlyTimelineStats(redis, "failed"));
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            return UseConnection(redis =>
            {
                var job = redis.GetAllEntriesFromHash(String.Format("hangfire:job:{0}", jobId));
                if (job.Count == 0) return null;

                var hiddenProperties = new[] { "Type", "Method", "ParameterTypes", "Arguments", "State", "CreatedAt" };

                var historyList = redis.GetAllItemsFromList(
                    String.Format("hangfire:job:{0}:history", jobId));

                var history = historyList
                    .Select(JobHelper.FromJson<Dictionary<string, string>>)
                    .ToList();

                var stateHistory = new List<StateHistoryDto>(history.Count);
                foreach (var entry in history)
                {
                    var dto = new StateHistoryDto
                    {
                        StateName = entry["State"],
                        Reason = entry.ContainsKey("Reason") ? entry["Reason"] : null,
                        CreatedAt = JobHelper.DeserializeDateTime(entry["CreatedAt"]),
                    };

                    // Each history item contains all of the information,
                    // but other code should not know this. We'll remove
                    // unwanted keys.
                    var stateData = new Dictionary<string, string>(entry);
                    stateData.Remove("State");
                    stateData.Remove("Reason");
                    stateData.Remove("CreatedAt");

                    dto.Data = stateData;
                    stateHistory.Add(dto);
                }

                // For compatibility
                if (!job.ContainsKey("Method")) job.Add("Method", null);
                if (!job.ContainsKey("ParameterTypes")) job.Add("ParameterTypes", null);

                return new JobDetailsDto
                {
                    Job = TryToGetJob(job["Type"], job["Method"], job["ParameterTypes"], job["Arguments"]),
                    CreatedAt =
                        job.ContainsKey("CreatedAt")
                            ? JobHelper.DeserializeDateTime(job["CreatedAt"])
                            : (DateTime?) null,
                    Properties =
                        job.Where(x => !hiddenProperties.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value),
                    History = stateHistory
                };
            });
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(
            IRedisClient redis, string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keys = dates.Select(x => String.Format("hangfire:stats:{0}:{1}", type, x.ToString("yyyy-MM-dd-HH"))).ToList();
            var valuesMap = redis.GetValuesMap(keys);

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < dates.Count; i++)
            {
                long value;
                if (!long.TryParse(valuesMap[valuesMap.Keys.ElementAt(i)], out value))
                {
                    value = 0;
                }

                result.Add(dates[i], value);
            }

            return result;
        }

        private Dictionary<DateTime, long> GetTimelineStats(
            IRedisClient redis, string type)
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
            var keys = stringDates.Select(x => String.Format("hangfire:stats:{0}:{1}", type, x)).ToList();

            var valuesMap = redis.GetValuesMap(keys);

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < stringDates.Count; i++)
            {
                long value;
                if (!long.TryParse(valuesMap[valuesMap.Keys.ElementAt(i)], out value))
                {
                    value = 0;
                }
                result.Add(dates[i], value);
            }

            return result;
        }

        private JobList<T> GetJobsWithProperties<T>(
            IRedisClient redis,
            IList<string> jobIds,
            string[] properties,
            string[] stateProperties,
            Func<Job, List<string>, List<string>, T> selector)
        {
            if (jobIds.Count == 0) return new JobList<T>(new List<KeyValuePair<string, T>>());

            var jobs = new Dictionary<string, List<string>>(jobIds.Count);
            var states = new Dictionary<string, List<string>>(jobIds.Count);

            properties = properties ?? new string[0];

            using (var pipeline = redis.CreatePipeline())
            {
                foreach (var jobId in jobIds)
                {
                    var id = jobId;

                    pipeline.QueueCommand(
                        x => x.GetValuesFromHash(
                            String.Format("hangfire:job:{0}", id),
                            properties.Union(new[] { "Type", "Method", "ParameterTypes", "Arguments" }).ToArray()),
                        x => { if (!jobs.ContainsKey(id)) jobs.Add(id, x); });

                    if (stateProperties != null)
                    {
                        pipeline.QueueCommand(
                            x => x.GetValuesFromHash(String.Format("hangfire:job:{0}:state", id), stateProperties),
                            x => { if (!states.ContainsKey(id)) states.Add(id, x); });
                    }
                }

                pipeline.Flush();
            }

            return new JobList<T>(jobIds
                .Select(x => new
                {
                    JobId = x,
                    Job = jobs[x],
                    Method = TryToGetJob(
                        jobs[x][properties.Length],
                        jobs[x][properties.Length + 1],
                        jobs[x][properties.Length + 2],
                        jobs[x][properties.Length + 3]),
                    State = states.ContainsKey(x) ? states[x] : null
                })
                .Select(x => new KeyValuePair<string, T>(
                    x.JobId,
                    x.Job.TrueForAll(y => y == null)
                        ? default(T)
                        : selector(x.Method, x.Job, x.State)))
                .ToList());
        }

        public long SucceededListCount()
        {
            return UseConnection(redis => redis.GetListCount("hangfire:succeeded"));
        }

        public StatisticsDto GetStatistics()
        {
            return UseConnection(redis =>
            {
                var stats = new StatisticsDto();

                var queues = redis.GetAllItemsFromSet("hangfire:queues");

                using (var pipeline = redis.CreatePipeline())
                {
                    pipeline.QueueCommand(
                        x => x.GetSetCount("hangfire:servers"),
                        x => stats.Servers = x);

                    pipeline.QueueCommand(
                        x => x.GetSetCount("hangfire:queues"),
                        x => stats.Queues = x);

                    pipeline.QueueCommand(
                        x => x.GetSortedSetCount("hangfire:schedule"),
                        x => stats.Scheduled = x);

                    pipeline.QueueCommand(
                        x => x.GetSortedSetCount("hangfire:processing"),
                        x => stats.Processing = x);

                    pipeline.QueueCommand(
                        x => x.GetValue("hangfire:stats:succeeded"),
                        x => stats.Succeeded = long.Parse(x ?? "0"));

                    pipeline.QueueCommand(
                        x => x.GetSortedSetCount("hangfire:failed"),
                        x => stats.Failed = x);

                    pipeline.QueueCommand(
                        x => x.GetValue("hangfire:stats:deleted"),
                        x => stats.Deleted = long.Parse(x ?? "0"));

                    pipeline.QueueCommand(
                        x => x.GetSortedSetCount("hangfire:recurring-jobs"),
                        x => stats.Recurring = x);

                    foreach (var queue in queues)
                    {
                        var queueName = queue;
                        pipeline.QueueCommand(
                            x => x.GetListCount(String.Format("hangfire:queue:{0}", queueName)),
                            x => stats.Enqueued += x);
                    }

                    pipeline.Flush();
                }

                return stats;
            });
        }

        private T UseConnection<T>(Func<IRedisClient, T> action)
        {
            using (var redis = _connectionPool.GetClient())
            {
                return action(redis);
            }
        }

        private static Job TryToGetJob(
            string type, string method, string parameterTypes, string arguments)
        {
            try
            {
                return new InvocationData(
                    type,
                    method,
                    parameterTypes,
                    arguments).Deserialize();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
