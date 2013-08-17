using System;
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
    }
}
