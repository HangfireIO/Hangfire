using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ServiceStack.Redis;

namespace HangFire
{
    internal class RedisStorage : IDisposable
    {
        private readonly TimeSpan _workerStatusTimeout = TimeSpan.FromDays(1);
        private readonly HangFireConfiguration _config = HangFireConfiguration.Current;
        private readonly IRedisClient _redis;

        public RedisStorage()
        {
            _redis = new RedisClient(_config.RedisHost, _config.RedisPort, _config.RedisPassword, _config.RedisDb); 
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        public void RetryOnRedisException(Action<RedisStorage> action)
        {
            while (true)
            {
                try
                {
                    action(this);
                    return;
                }
                catch (IOException)
                {
                    // TODO: log
                }
                catch (RedisException)
                {
                    // TODO: log
                }
            }
        }

        public void ScheduleJob(string jobId, Dictionary<string, string> job, double at)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:job:{0}", jobId),
                    job));

                transaction.QueueCommand(x => x.SetEntryInHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "ScheduledAt",
                    JsonHelper.Serialize(DateTime.UtcNow)));

                transaction.QueueCommand(x => x.AddItemToSortedSet(
                    "hangfire:schedule", jobId, at));

                transaction.Commit();
            }
        }

        public string GetScheduledJobId(double now)
        {
            var jobId = _redis
                .GetRangeFromSortedSetByLowestScore("hangfire:schedule", Double.NegativeInfinity, now, 0, 1)
                .FirstOrDefault();

            if (jobId != null)
            {
                if (_redis.RemoveItemFromSortedSet("hangfire:schedule", jobId))
                {
                    return jobId;
                }
            }

            return null;
        }

        public void EnqueueJob(string queueName, string jobId, Dictionary<string, string> job)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                if (job != null)
                {
                    transaction.QueueCommand(x => x.SetRangeInHash(
                        String.Format("hangfire:job:{0}", jobId),
                        job));
                }

                transaction.QueueCommand(x => x.SetEntryInHashIfNotExists(
                    String.Format("hangfire:job:{0}", jobId),
                    "EnqueuedAt",
                    JsonHelper.Serialize(DateTime.UtcNow)));

                transaction.QueueCommand(x => x.AddItemToSet("hangfire:queues", queueName));
                transaction.QueueCommand(x => x.EnqueueItemOnList(
                    String.Format("hangfire:queue:{0}", queueName),
                    jobId));

                transaction.Commit();
            }
        }

        public string DequeueJobId(string serverName, string queue, TimeSpan? timeOut)
        {
            return _redis.BlockingPopAndPushItemBetweenLists(
                    String.Format("hangfire:queue:{0}", queue),
                    String.Format("hangfire:processing:{0}:{1}", serverName, queue),
                    timeOut);
        }

        public string GetJobType(string jobId)
        {
            return _redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", jobId),
                "Type");
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

        public void RemoveProcessingJob(string serverName, string queue, string jobId)
        {
            _redis.RemoveItemFromList(
                String.Format("hangfire:processing:{0}:{1}", serverName, queue),
                jobId,
                -1);
        }

        public void AddProcessingWorker(string workerName, string jobId)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:processing"));
                transaction.QueueCommand(x =>
                    x.AddItemToSet("hangfire:workers", workerName));

                transaction.QueueCommand(x => x.SetEntry(
                    String.Format("hangfire:worker:{0}", workerName),
                    jobId));

                transaction.QueueCommand(x => x.SetEntryInHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "StartedAt",
                    JsonHelper.Serialize(DateTime.UtcNow)));

                transaction.QueueCommand(x => x.ExpireEntryIn(
                    String.Format("hangfire:worker:{0}", workerName), 
                    _workerStatusTimeout));

                transaction.Commit();
            }
        }

        public void RemoveProcessingWorker(string workerName, string jobId, Exception exception)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.DecrementValue("hangfire:stats:processing"));

                transaction.QueueCommand(x =>
                    x.RemoveItemFromSet("hangfire:workers", workerName));
                transaction.QueueCommand(x =>
                    x.RemoveEntry(String.Format("hangfire:workers:{0}", workerName)));

                if (exception == null)
                {
                    transaction.QueueCommand(x => x.SetEntryInHash(
                        String.Format("hangfire:job:{0}", jobId),
                        "SucceededAt",
                        JsonHelper.Serialize(DateTime.UtcNow)));

                    transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:succeeded"));
                    transaction.QueueCommand(x => x.IncrementValue(
                        String.Format("hangfire:stats:succeeded:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

                    transaction.QueueCommand(x => x.PushItemToList("hangfire:succeeded", jobId));
                    transaction.QueueCommand(x => x.TrimList("hangfire:succeeded", 0, 99));

                    var hourlySucceededKey = String.Format(
                        "hangfire:stats:succeeded:{0}",
                        DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));
                    transaction.QueueCommand(x => x.IncrementValue(hourlySucceededKey));
                    transaction.QueueCommand(x => x.ExpireEntryIn(hourlySucceededKey, TimeSpan.FromDays(1)));
                }
                else
                {
                    transaction.QueueCommand(x => x.SetEntryInHash(
                        String.Format("hangfire:job:{0}", jobId),
                        "FailedAt",
                        JsonHelper.Serialize(DateTime.UtcNow)));

                    transaction.QueueCommand(x => x.SetRangeInHash(
                        String.Format("hangfire:job:{0}", jobId),
                        new Dictionary<string, string>
                            {
                                { "ExceptionType", exception.GetType().FullName },
                                { "ExceptionMessage", exception.Message },
                                { "StackTrace", exception.StackTrace }
                            }));

                    transaction.QueueCommand(x => x.AddItemToSortedSet(
                        "hangfire:failed",
                        jobId,
                        DateTime.UtcNow.ToTimestamp()));

                    transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:failed"));
                    transaction.QueueCommand(x => x.IncrementValue(
                        String.Format("hangfire:stats:failed:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
                    
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

        public IEnumerable<WorkerDto> GetWorkers()
        {
            var workers = _redis.GetAllItemsFromSet("hangfire:workers");
            var result = new List<WorkerDto>();
            foreach (var workerName in workers)
            {
                var jobId = _redis.GetValue(String.Format("hangfire:worker:{0}", workerName));
                var job = _redis.GetValuesFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    new[] { "Type", "Args", "StartedAt" });

                result.Add(new WorkerDto
                    {
                        Name = workerName,
                        Args = job[1],
                        Type = job[0],
                        StartedAt = job[2]
                    });
            }

            return result;
        }

        public IList<ScheduleDto> GetSchedule()
        {
            // TODO: use ZRANGEBYSCORE and split results into pages.
            var scheduledJobs = _redis.GetAllWithScoresFromSortedSet("hangfire:schedule");
            var result = new List<ScheduleDto>();

            foreach (var scheduledJob in scheduledJobs)
            {
                var job = _redis.GetValuesFromHash(
                    String.Format("hangfire:job:{0}", scheduledJob.Key),
                    new[] { "Type", "Args" });

                result.Add(new ScheduleDto
                    {
                        TimeStamp = scheduledJob.Value.ToString(),
                        Args = job[1],
                        Queue = JobHelper.GetQueueName(job[0]),
                        Type = job[0]
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
            // TODO: use LRANGE and pages.
            var failedJobIds = _redis.GetAllItemsFromSortedSetDesc("hangfire:failed");
            var result = new List<FailedJobDto>(failedJobIds.Count);

            foreach (var jobId in failedJobIds)
            {
                var job = _redis.GetValuesFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    new[] { "Type", "Args", "FailedAt", "ExceptionType", "ExceptionMessage", "StackTrace" });

                result.Add(new FailedJobDto
                    {
                        Type = job[0],
                        Queue = JobHelper.GetQueueName(job[0]),
                        Args = JsonHelper.Deserialize<Dictionary<string, string>>(job[1]),
                        FailedAt = JsonHelper.Deserialize<DateTime>(job[2]),
                        ExceptionType = job[3],
                        ExceptionMessage = job[4],
                        ExceptionStackTrace = job[5],
                    });
            }

            return result.OrderByDescending(x => x.FailedAt).ToList();
        }

        public IList<SucceededJobDto> GetSucceededJobs()
        {
            // TODO: use LRANGE with paging.
            var succeededJobIds = _redis.GetAllItemsFromList("hangfire:succeeded");
            var result = new List<SucceededJobDto>(succeededJobIds.Count);

            foreach (var jobId in succeededJobIds)
            {
                var job = _redis.GetValuesFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    new[] { "Type", "Args", "SucceededAt" });

                result.Add(new SucceededJobDto
                    {
                        Type = job[0],
                        Queue = JobHelper.GetQueueName(job[0]),
                        Args = JsonHelper.Deserialize<Dictionary<string, string>>(job[1]),
                        SucceededAt = JsonHelper.Deserialize<DateTime>(job[2]),
                        Latency = TimeSpan.FromSeconds(2) // TODO: replace with the correct value.
                    });
            }

            return result;
        }

        public void GetJobTypeAndArgs(string jobId, out string jobType, out Dictionary<string, string> jobArgs)
        {
            var result = _redis.GetValuesFromHash(
                String.Format("hangfire:job:{0}", jobId),
                new[] { "Type", "Args" });

            jobType = result[0];
            jobArgs = JsonHelper.Deserialize<Dictionary<string, string>>(result[1]);
        }

        public void SetJobProperty(string jobId, string propertyName, object value)
        {
            _redis.SetEntryInHash(
                String.Format("hangfire:job:{0}", jobId),
                propertyName,
                JsonHelper.Serialize(value));
        }

        public T GetJobProperty<T>(string jobId, string propertyName)
        {
            var value = _redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", jobId),
                propertyName);

            return JsonHelper.Deserialize<T>(value);
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

    public class WorkerDto
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
