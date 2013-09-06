using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ServiceStack.Redis;

namespace HangFire
{
    internal class RedisStorage
    {
        private readonly IRedisClient _redis;

        public RedisStorage(IRedisClient redis)
        {
            _redis = redis;
        }

        public void ScheduleJob(string job, double at)
        {
            _redis.AddItemToSortedSet("hangfire:schedule", job, at);
        }

        public string GetScheduledJob(double now)
        {
            var scheduledJob =
                _redis.GetRangeFromSortedSetByLowestScore("hangfire:schedule", Double.NegativeInfinity, now, 0, 1)
                    .FirstOrDefault();

            if (scheduledJob != null)
            {
                if (_redis.RemoveItemFromSortedSet("hangfire:schedule", scheduledJob))
                {
                    return scheduledJob;
                }
            }

            return null;
        }

        public void EnqueueJob(string queue, string job)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.AddItemToSet("hangfire:queues", queue));
                transaction.QueueCommand(x => x.EnqueueItemOnList(
                    String.Format("hangfire:queue:{0}", queue), job));
                transaction.Commit();
            }
        }

        public string DequeueJob(string serverName, string queue, TimeSpan? timeOut)
        {
            return _redis.BlockingPopAndPushItemBetweenLists(
                    String.Format("hangfire:queue:{0}", queue),
                    String.Format("hangfire:processing:{0}:{1}", serverName, queue),
                    timeOut);
        }

        public int RequeueProcessingJobs(string serverName, string currentQueue, CancellationToken cancellationToken)
        {
            var queues = _redis.GetAllItemsFromSet(String.Format("hangfire:server:{0}:queues", serverName));

            int requeued = 0;

            foreach (var queue in queues)
            {
                while (_redis.PopAndPushItemBetweenLists(
                    String.Format("hangfire:processing:{0}:{1}", serverName, queue),
                    String.Format("hangfire:queue:{0}", queue)) != null)
                {
                    requeued++;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                using (var transaction = _redis.CreateTransaction())
                {
                    transaction.QueueCommand(x => x.RemoveEntry(
                        String.Format("hangfire:server:{0}:queues", serverName)));
                    transaction.QueueCommand(x => x.AddItemToSet(
                        String.Format("hangfire:server:{0}:queues", serverName), currentQueue));
                    transaction.Commit();
                }
            }

            return requeued;
        }

        public void RemoveProcessingJob(string serverName, string queue, string job)
        {
            _redis.RemoveItemFromList(
                String.Format("hangfire:processing:{0}:{1}", serverName, queue),
                job,
                -1);
        }

        public void AddProcessingDispatcher(string name, string type, string args)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:processing"));

                transaction.QueueCommand(x =>
                    x.AddItemToSet("hangfire:dispatchers", name));
                transaction.QueueCommand(x =>
                    x.SetEntryInHash(String.Format("hangfire:dispatcher:{0}", name), "type", type));
                transaction.QueueCommand(x => 
                    x.SetEntryInHash(String.Format("hangfire:dispatcher:{0}", name), "args", args));
                transaction.QueueCommand(x =>
                    x.SetEntryInHash(String.Format("hangfire:dispatcher:{0}", name), "started-at", DateTime.UtcNow.ToString()));
                transaction.QueueCommand(x =>
                    x.ExpireEntryIn(String.Format("hangfire:dispatcher:{0}", name), TimeSpan.FromSeconds(20)));

                transaction.Commit();
            }
        }

        public void RemoveProcessingDispatcher(string name, JobDescription jobDescription, Exception exception)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.DecrementValue("hangfire:stats:processing"));

                transaction.QueueCommand(x =>
                    x.RemoveItemFromSet("hangfire:dispatchers", name));
                transaction.QueueCommand(x =>
                    x.RemoveEntry(String.Format("hangfire:dispatcher:{0}", name)));

                if (exception == null)
                {
                    transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:succeeded"));
                    transaction.QueueCommand(x => x.IncrementValue(
                        String.Format("hangfire:stats:succeeded:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

                    transaction.QueueCommand(x => x.PushItemToList("hangfire:succeeded", jobDescription.Serialize()));
                    transaction.QueueCommand(x => x.TrimList("hangfire:succeeded", 0, 99));

                    var hourlySucceededKey = String.Format(
                        "hangfire:stats:succeeded:{0}",
                        DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));
                    transaction.QueueCommand(x => x.IncrementValue(hourlySucceededKey));
                    transaction.QueueCommand(x => x.ExpireEntryIn(hourlySucceededKey, TimeSpan.FromDays(1)));
                }
                else
                {
                    transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:failed"));
                    transaction.QueueCommand(x => x.IncrementValue(
                        String.Format("hangfire:stats:failed:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
                    transaction.QueueCommand(x => x.PushItemToList("hangfire:failed", jobDescription.Serialize()));

                    transaction.QueueCommand(x => x.IncrementValue(
                        String.Format("hangfire:stats:failed:{0}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm"))));

                    var hourlyFailedKey = String.Format(
                        "hangfire:stats:failed:{0}",
                        DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));
                    transaction.QueueCommand(x => x.IncrementValue(hourlyFailedKey));
                    transaction.QueueCommand(x => x.ExpireEntryIn(hourlyFailedKey, TimeSpan.FromDays(1)));
                }

                transaction.Commit();
            }
        }

        public long GetScheduledCount()
        {
            return _redis.GetSortedSetCount("hangfire:schedule");
        }

        public long GetEnqueuedCount()
        {
            var queues = _redis.GetAllItemsFromSet("hangfire:queues");
            return queues.Sum(queue => _redis.GetListCount(
                String.Format("hangfire:queue:{0}", queue)));
        }

        public long GetSucceededCount()
        {
            return long.Parse(
                _redis.GetValue("hangfire:stats:succeeded") ?? "0");
        }

        public long GetFailedCount()
        {
            return long.Parse(
                _redis.GetValue("hangfire:stats:failed") ?? "0");
        }

        public long GetProcessingCount()
        {
            return long.Parse(
                _redis.GetValue("hangfire:stats:processing") ?? "0");
        }

        public IEnumerable<QueueDto> GetQueues()
        {
            var queueNames = _redis.GetAllItemsFromSet("hangfire:queues");
            return queueNames.Select(queueName => new QueueDto
                {
                    Name = queueName, 
                    Length = _redis.GetListCount(String.Format("hangfire:queue:{0}", queueName)),
                    Servers = _redis.GetAllItemsFromSet(String.Format("hangfire:queue:{0}:servers", queueName))
                }).ToList();
        }

        public IEnumerable<DispatcherDto> GetDispatchers()
        {
            var dispatchers = _redis.GetAllItemsFromSet("hangfire:dispatchers");
            var result = new List<DispatcherDto>();
            foreach (var dispatcher in dispatchers)
            {
                var entry = _redis.GetAllEntriesFromHash(String.Format("hangfire:dispatcher:{0}", dispatcher));
                if (entry.Count == 0) continue;
                result.Add(new DispatcherDto
                    {
                        Name = dispatcher,
                        Args = entry["args"],
                        Type = entry["type"],
                        StartedAt = entry["started-at"]
                    });
            }

            return result;
        }

        public IList<ScheduleDto> GetSchedule()
        {
            var schedule = _redis.GetAllWithScoresFromSortedSet("hangfire:schedule");
            var result = new List<ScheduleDto>();
            foreach (var scheduled in schedule)
            {
                var job = JsonHelper.Deserialize<JobDescription>(scheduled.Key);
                result.Add(new ScheduleDto
                    {
                        TimeStamp = scheduled.Value.ToString(),
                        Args = JsonHelper.Serialize(job.Args),
                        Queue = JobHelper.GetQueueName(job.JobType),
                        Type = job.JobType
                    });
            }

            return result;
        }

        public void AnnounceServer(string serverName, int concurrency, string queue)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.AddItemToSet(
                    "hangfire:servers", serverName));
                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:server:{0}", serverName), 
                    new Dictionary<string, string>
                        {
                            { "server-name", serverName },
                            { "concurrency", concurrency.ToString() },
                            { "queue", queue },
                            { "started-at", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) }
                        }));
                transaction.QueueCommand(x => x.AddItemToSet(
                    String.Format("hangfire:queue:{0}:servers", queue), serverName));

                transaction.Commit();
            }
        }

        public void HideServer(string serverName, string queue)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromSet(
                    "hangfire:servers", serverName));
                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:server:{0}", serverName)));
                transaction.QueueCommand(x => x.RemoveItemFromSet(
                    String.Format("hangfire:queue:{0}:servers", queue), serverName));

                transaction.Commit();
            }
        }

        public Dictionary<string, long> GetSucceededByDatesCount()
        {
            return GetTimelineStats("succeeded");
        }

        public Dictionary<string, long> GetFailedByDatesCount()
        {
            return GetTimelineStats("failed");
        }

        public Dictionary<DateTime, long> GetHourlySucceededCount()
        {
            return GetHourlyTimelineStats("succeeded");
        }

        public Dictionary<DateTime, long> GetHourlyFailedCount()
        {
            return GetHourlyTimelineStats("failed");
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

            var keys = dates.Select(x => String.Format("hangfire:stats:{0}:{1}", type, x.ToString("yyyy-MM-dd-HH"))).ToList();
            var valuesMap = _redis.GetValuesMap(keys);

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

        private Dictionary<string, long> GetTimelineStats(string type)
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

            var valuesMap = _redis.GetValuesMap(keys);

            var result = new Dictionary<string, long>();
            for (var i = 0; i < stringDates.Count; i++)
            {
                long value;
                if (!long.TryParse(valuesMap[valuesMap.Keys.ElementAt(i)], out value))
                {
                    value = 0;
                }
                result.Add(stringDates[i], value);
            }

            return result;
        }
         
        public IList<ServerDto> GetServers()
        {
            var serverNames = _redis.GetAllItemsFromSet("hangfire:servers");
            var result = new List<ServerDto>(serverNames.Count);
            foreach (var serverName in serverNames)
            {
                var server = _redis.GetAllEntriesFromHash(
                    String.Format("hangfire:server:{0}", serverName));
                if (server.Count > 0)
                {
                    result.Add(new ServerDto
                        {
                            Name = serverName,
                            Queue = server["queue"],
                            Concurrency = int.Parse(server["concurrency"]),
                            StartedAt = server["started-at"]
                        });
                }
            }

            return result;
        }

        public IList<FailedJobDto> GetFailedJobs()
        {
            var failed = _redis.GetAllItemsFromList("hangfire:failed");
            return failed.Select(JsonHelper.Deserialize<JobDescription>)
                .Reverse()
                .Select(x => new FailedJobDto
                {
                    Args = new Dictionary<string, string>(x.Args),
                    Queue = JobHelper.GetQueueName(x.JobType),
                    Type = x.JobType,
                    FailedAt = x.FailedAt,
                    Latency = x.Latency,
                    ExceptionType = x.Properties["ExceptionType"],
                    ExceptionMessage = x.Properties["ExceptionMessage"],
                    ExceptionStackTrace = x.Properties["StackTrace"],
                })
                .ToList();
        }

        public IList<SucceededJobDto> GetSucceededJobs()
        {
            var succeeded = _redis.GetAllItemsFromList("hangfire:succeeded");
            return succeeded.Select(JobDescription.Deserialize)
                .Reverse()
                .Select(x => new SucceededJobDto
                {
                    Args = new Dictionary<string, string>(x.Args),
                    Queue = JobHelper.GetQueueName(x.JobType),
                    Type = x.JobType,
                    SucceededAt = x.SucceededAt,
                    Latency = x.Latency
                })
                .ToList();
        }
    }

    public class ServerDto
    {
        public string Name { get; set; }
        public int Concurrency { get; set; }
        public string Queue { get; set; }
        public string StartedAt { get; set; }
    }

    public class QueueDto
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public HashSet<string> Servers { get; set; }
    }

    public class DispatcherDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Args { get; set; }
        public string StartedAt { get; set; }
    }

    public class ScheduleDto
    {
        public string TimeStamp { get; set; }
        public string Type { get; set; }
        public string Queue { get; set; }
        public string Args { get; set; }
    }

    public class FailedJobDto
    {
        public string Type { get; set; }
        public string Queue { get; set; }
        public Dictionary<String, String> Args { get; set; }
        public DateTime? FailedAt { get; set; }
        public TimeSpan Latency { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionStackTrace { get; set; }
    }

    public class SucceededJobDto
    {
        public string Type { get; set; }
        public string Queue { get; set; }
        public Dictionary<String, String> Args { get; set; }
        public DateTime? SucceededAt { get; set; }
        public TimeSpan Latency { get; set; }
    }
}
