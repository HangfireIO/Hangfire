using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public string DequeueJob(string iid, string queue, TimeSpan? timeOut)
        {
            return _redis.BlockingPopAndPushItemBetweenLists(
                    String.Format("hangfire:queue:{0}", queue),
                    String.Format("hangfire:processing:{0}:{1}", iid, queue),
                    timeOut);
        }

        public int RequeueProcessingJobs(string iid, string currentQueue, CancellationToken cancellationToken)
        {
            var queues = _redis.GetAllItemsFromSet(String.Format("hangfire:server:{0}:queues", iid));

            int requeued = 0;

            foreach (var queue in queues)
            {
                while (_redis.PopAndPushItemBetweenLists(
                    String.Format("hangfire:processing:{0}:{1}", iid, queue),
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
                        String.Format("hangfire:server:{0}:queues", iid)));
                    transaction.QueueCommand(x => x.AddItemToSet(
                        String.Format("hangfire:server:{0}:queues", iid), currentQueue));
                    transaction.Commit();
                }
            }

            return requeued;
        }

        public void RemoveProcessingJob(string iid, string queue, string job)
        {
            _redis.RemoveItemFromList(
                String.Format("hangfire:processing:{0}:{1}", iid, queue),
                job,
                -1);
        }

        public void AddProcessingDispatcher(string name, string type, string args)
        {
            using (var transaction = _redis.CreateTransaction())
            {
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

        public void RemoveProcessingDispatcher(string name)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x =>
                    x.RemoveItemFromSet("hangfire:dispatchers", name));
                transaction.QueueCommand(x =>
                    x.RemoveEntry(String.Format("hangfire:dispatcher:{0}", name)));
                transaction.Commit();
            }
        }

        public void IncreaseSucceeded()
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:succeeded"));
                transaction.QueueCommand(x => x.IncrementValue(
                    String.Format("hangfire:stats:succeeded:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
                transaction.Commit();
            }
        }

        public void IncrementFailed()
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:failed"));
                transaction.QueueCommand(x => x.IncrementValue(
                    String.Format("hangfire:stats:failed:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
                transaction.Commit();
            }
        }

        public void IncreaseProcessing()
        {
            _redis.IncrementValue("hangfire:stats:processing");
        }

        public void DecreaseProcessing()
        {
            _redis.DecrementValue("hangfire:stats:processing");
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
                    Length = _redis.GetListCount(String.Format("hangfire:queue:{0}", queueName))
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

        public IEnumerable<ScheduleDto> GetSchedule()
        {
            var schedule = _redis.GetAllWithScoresFromSortedSet("hangfire:schedule");
            var result = new List<ScheduleDto>();
            foreach (var scheduled in schedule)
            {
                var job = JsonHelper.Deserialize<Job>(scheduled.Key);
                result.Add(new ScheduleDto
                    {
                        TimeStamp = scheduled.Value.ToString(),
                        Args = JsonHelper.Serialize(job.Args),
                        Queue = Worker.GetQueueName(job.WorkerType),
                        Type = job.WorkerType.Name
                    });
            }

            return result;
        }

        public Dictionary<string, long> GetSucceededByDatesCount()
        {
            return GetTimelineStats("succeeded");
        }

        public Dictionary<string, long> GetFailedByDatesCount()
        {
            return GetTimelineStats("failed");
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
    }

    public class QueueDto
    {
        public string Name { get; set; }
        public long Length { get; set; }
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
}
