// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Web
{
    internal static class JobStorage
    {
        private static readonly IRedisClient Redis = RedisFactory.BasicManager.GetClient();

        public static string TryToGetQueue(string jobType)
        {
            var type = Type.GetType(jobType);
            if (type == null)
            {
                return null;
            }

            return EnqueuedState.GetQueue(type);
        }

        public static long ScheduledCount()
        {
            lock (Redis)
            {
                return Redis.GetSortedSetCount("hangfire:schedule");
            }
        }

        public static long EnqueuedCount(string queue)
        {
            lock (Redis)
            {
                return Redis.GetListCount(String.Format("hangfire:queue:{0}", queue));
            }
        }

        public static long DequeuedCount(string queue)
        {
            lock (Redis)
            {
                return Redis.GetListCount(String.Format("hangfire:queue:{0}:dequeued", queue));
            }
        }

        public static long FailedCount()
        {
            lock (Redis)
            {
                return Redis.GetSortedSetCount("hangfire:failed");
            }
        }

        public static long ProcessingCount()
        {
            lock (Redis)
            {
                return Redis.GetSortedSetCount("hangfire:processing");
            }
        }

        public static IList<KeyValuePair<string, ProcessingJobDto>> ProcessingJobs(
            int from, int count)
        {
            lock (Redis)
            {
                var jobIds = Redis.GetRangeFromSortedSet(
                    "hangfire:processing",
                    from,
                    from + count - 1);

                return GetJobsWithProperties(Redis,
                    jobIds,
                    new[] { "Type" },
                    new[] { "StartedAt", "ServerName", "State" },
                    (job, state) => new ProcessingJobDto
                    {
                        ServerName = state[1],
                        Type = job[0],
                        Queue = TryToGetQueue(job[0]),
                        StartedAt = JobHelper.FromNullableStringTimestamp(state[0]),
                        InProcessingState = ProcessingState.Name.Equals(
                            state[2], StringComparison.OrdinalIgnoreCase),
                        State = state[2]
                    }).OrderBy(x => x.Value.StartedAt).ToList();
            }
        }

        public static IDictionary<string, ScheduleDto> ScheduledJobs(int from, int count)
        {
            lock (Redis)
            {
                var scheduledJobs = Redis.GetRangeWithScoresFromSortedSet(
                    "hangfire:schedule",
                    from,
                    from + count - 1);

                if (scheduledJobs.Count == 0)
                {
                    return new Dictionary<string, ScheduleDto>();
                }

                var jobs = new Dictionary<string, List<string>>();
                var states = new Dictionary<string, string>();

                using (var pipeline = Redis.CreatePipeline())
                {
                    foreach (var scheduledJob in scheduledJobs)
                    {
                        var job = scheduledJob;

                        pipeline.QueueCommand(
                            x => x.GetValuesFromHash(
                                String.Format("hangfire:job:{0}", job.Key),
                                new[] { "Type" }),
                            x => jobs.Add(job.Key, x));

                        pipeline.QueueCommand(
                            x => x.GetValueFromHash(
                                String.Format("hangfire:job:{0}:state", job.Key),
                                "State"),
                            x => states.Add(job.Key, x));
                    }

                    pipeline.Flush();
                }

                return scheduledJobs.ToDictionary(
                    job => job.Key,
                    job => new ScheduleDto
                    {
                        ScheduledAt = JobHelper.FromTimestamp((long) job.Value),
                        Queue = TryToGetQueue(jobs[job.Key][0]),
                        Type = jobs[job.Key][0],
                        InScheduledState =
                            ScheduledState.Name.Equals(states[job.Key], StringComparison.OrdinalIgnoreCase)
                    });
            }
        }

        public static IDictionary<DateTime, long> SucceededByDatesCount()
        {
            lock (Redis)
            {
                return GetTimelineStats(Redis, "succeeded");
            }
        }

        public static IDictionary<DateTime, long> FailedByDatesCount()
        {
            lock (Redis)
            {
                return GetTimelineStats(Redis, "failed");
            }
        }

        public static IList<ServerDto> Servers()
        {
            lock (Redis)
            {
                var serverNames = Redis.GetAllItemsFromSet("hangfire:servers");

                if (serverNames.Count == 0)
                {
                    return new List<ServerDto>();
                }

                var servers = new Dictionary<string, List<string>>();
                var queues = new Dictionary<string, List<string>>();

                using (var pipeline = Redis.CreatePipeline())
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
                    StartedAt = JobHelper.FromStringTimestamp(servers[x][1]),
                    Heartbeat = JobHelper.FromNullableStringTimestamp(servers[x][2])
                }).ToList();
            }
        }

        public static IList<KeyValuePair<string, FailedJobDto>> FailedJobs(int from, int count)
        {
            lock (Redis)
            {
                var failedJobIds = Redis.GetRangeFromSortedSetDesc(
                    "hangfire:failed",
                    from,
                    from + count - 1);

                return GetJobsWithProperties(
                    Redis,
                    failedJobIds,
                    new[] { "Type", "Args" },
                    new[] { "FailedAt", "ExceptionType", "ExceptionMessage", "ExceptionDetails", "State" },
                    (job, state) => new FailedJobDto
                    {
                        Type = job[0],
                        Queue = TryToGetQueue(job[0]),
                        Args = JobHelper.FromJson<Dictionary<string, string>>(job[1]),
                        FailedAt = JobHelper.FromNullableStringTimestamp(state[0]),
                        ExceptionType = state[1],
                        ExceptionMessage = state[2],
                        ExceptionDetails = state[3],
                        InFailedState = FailedState.Name.Equals(state[4], StringComparison.OrdinalIgnoreCase)
                    });
            }
        }

        public static IList<KeyValuePair<string, SucceededJobDto>> SucceededJobs(int from, int count)
        {
            lock (Redis)
            {
                var succeededJobIds = Redis.GetRangeFromList(
                    "hangfire:succeeded",
                    from,
                    from + count - 1);

                return GetJobsWithProperties(
                    Redis,
                    succeededJobIds,
                    new[] { "Type" },
                    new[] { "SucceededAt", "State" },
                    (job, state) => new SucceededJobDto
                    {
                        Type = job[0],
                        Queue = TryToGetQueue(job[0]),
                        SucceededAt = JobHelper.FromNullableStringTimestamp(state[0]),
                        InSucceededState = SucceededState.Name.Equals(state[1], StringComparison.OrdinalIgnoreCase)
                    });
            }
        }

        public static IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            lock (Redis)
            {
                var queues = Redis.GetAllItemsFromSet("hangfire:queues");
                var result = new List<QueueWithTopEnqueuedJobsDto>(queues.Count);

                foreach (var queue in queues)
                {
                    IList<string> firstJobIds = null;
                    long length = 0;
                    long dequeued = 0;

                    using (var pipeline = Redis.CreatePipeline())
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
                            x => dequeued = x);

                        pipeline.Flush();
                    }

                    var jobs = GetJobsWithProperties(
                        Redis,
                        firstJobIds,
                        new[] { "Type" },
                        new[] { "EnqueuedAt", "State" },
                        (job, state) => new EnqueuedJobDto
                        {
                            Type = job[0],
                            EnqueuedAt = JobHelper.FromNullableStringTimestamp(state[0]),
                            InEnqueuedState = EnqueuedState.Name.Equals(state[1], StringComparison.OrdinalIgnoreCase)
                        });

                    result.Add(new QueueWithTopEnqueuedJobsDto
                    {
                        Name = queue,
                        FirstJobs = jobs,
                        Length = length,
                        Dequeued = dequeued
                    });
                }

                return result;
            }
        }

        public static IList<KeyValuePair<string, EnqueuedJobDto>> EnqueuedJobs(
            string queue, int from, int perPage)
        {
            lock (Redis)
            {
                var jobIds = Redis.GetRangeFromList(
                    String.Format("hangfire:queue:{0}", queue),
                    from,
                    from + perPage - 1);

                return GetJobsWithProperties(
                    Redis,
                    jobIds,
                    new[] { "Type" },
                    new[] { "EnqueuedAt", "State" },
                    (job, state) => new EnqueuedJobDto
                    {
                        Type = job[0],
                        EnqueuedAt = JobHelper.FromNullableStringTimestamp(state[0]),
                        InEnqueuedState = EnqueuedState.Name.Equals(state[1], StringComparison.OrdinalIgnoreCase)
                    });
            }
        }

        public static IList<KeyValuePair<string, DequeuedJobDto>> DequeuedJobs(
            string queue, int from, int perPage)
        {
            lock (Redis)
            {
                var jobIds = Redis.GetRangeFromList(
                    String.Format("hangfire:queue:{0}:dequeued", queue),
                    from, from + perPage - 1);

                return GetJobsWithProperties(
                    Redis,
                    jobIds,
                    new[] { "Type", "State", "CreatedAt", "Fetched", "Checked" },
                    null,
                    (job, state) => new DequeuedJobDto
                    {
                        Type = job[0],
                        State = job[1],
                        CreatedAt = JobHelper.FromNullableStringTimestamp(job[2]),
                        FetchedAt = JobHelper.FromNullableStringTimestamp(job[3]),
                        CheckedAt = JobHelper.FromNullableStringTimestamp(job[4])
                    });
            }
        }

        public static IDictionary<DateTime, long> HourlySucceededJobs()
        {
            lock (Redis)
            {
                return GetHourlyTimelineStats(Redis, "succeeded");
            }
        }

        public static IDictionary<DateTime, long> HourlyFailedJobs()
        {
            lock (Redis)
            {
                return GetHourlyTimelineStats(Redis, "failed");
            }
        }

        public static bool RetryJob(string jobId)
        {
            lock (Redis)
            {
                // TODO: clear retry attempts counter.

                var stateMachine = new StateMachine(Redis);
                var state = new EnqueuedState("The job has been retried by a user.");

                return stateMachine.ChangeState(jobId, state, FailedState.Name);
            }
        }

        public static bool EnqueueScheduled(string jobId)
        {
            lock (Redis)
            {
                var stateMachine = new StateMachine(Redis);
                var state = new EnqueuedState("The job has been enqueued by a user.");

                return stateMachine.ChangeState(jobId, state, ScheduledState.Name);
            }
        }

        public static JobDetailsDto JobDetails(string jobId)
        {
            lock (Redis)
            {
                var job = Redis.GetAllEntriesFromHash(String.Format("hangfire:job:{0}", jobId));
                if (job.Count == 0) return null;

                var hiddenProperties = new[] { "Type", "Args", "State" };

                var historyList = Redis.GetAllItemsFromList(
                    String.Format("hangfire:job:{0}:history", jobId));

                var history = historyList
                    .Select(JobHelper.FromJson<Dictionary<string, string>>)
                    .ToList();

                return new JobDetailsDto
                {
                    Type = job["Type"],
                    Arguments = JobHelper.FromJson<Dictionary<string, string>>(job["Args"]),
                    State = job.ContainsKey("State") ? job["State"] : null,
                    Properties = job.Where(x => !hiddenProperties.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value),
                    History = history
                };
            }
        }

        private static Dictionary<DateTime, long> GetHourlyTimelineStats(
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

        private static Dictionary<DateTime, long> GetTimelineStats(
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

        private static IList<KeyValuePair<string, T>> GetJobsWithProperties<T>(
            IRedisClient redis,
            IList<string> jobIds,
            string[] properties,
            string[] stateProperties,
            Func<List<string>, List<string>, T> selector)
        {
            if (jobIds.Count == 0) return new List<KeyValuePair<string, T>>();

            var jobs = new Dictionary<string, List<string>>(jobIds.Count);
            var states = new Dictionary<string, List<string>>(jobIds.Count);

            using (var pipeline = redis.CreatePipeline())
            {
                foreach (var jobId in jobIds)
                {
                    var id = jobId;

                    pipeline.QueueCommand(
                        x => x.GetValuesFromHash(String.Format("hangfire:job:{0}", id), properties),
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

            return jobIds
                .Select(x => new
                {
                    JobId = x,
                    Job = jobs[x],
                    State = states.ContainsKey(x) ? states[x] : null
                })
                .Select(x => new KeyValuePair<string, T>(
                    x.JobId,
                    x.Job.TrueForAll(y => y == null) ? default(T) : selector(x.Job, x.State)))
                .ToList();
        }

        public static long SucceededListCount()
        {
            lock (Redis)
            {
                return Redis.GetListCount("hangfire:succeeded");
            }
        }

        public static StatisticsDto GetStatistics()
        {
            lock (Redis)
            {
                var stats = new StatisticsDto();

                var queues = Redis.GetAllItemsFromSet("hangfire:queues");

                using (var pipeline = Redis.CreatePipeline())
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
            }
        }
    }
}
