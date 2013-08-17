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
            _redis.EnqueueItemOnList(
                String.Format("hangfire:queue:{0}", queue), job);
        }

        public string DequeueJob(string iid, string queue, TimeSpan? timeOut)
        {
            return _redis.BlockingPopAndPushItemBetweenLists(
                String.Format("hangfire:queue:{0}", queue),
                String.Format("hangfire:processing:{0}:{1}", iid, queue),
                timeOut);
        }

        public int RequeueProcessingJobs(string iid, string queue, CancellationToken cancellationToken)
        {
            int requeued = 0;

            // TODO: А вдруг при перезапуске изменится имя очереди?
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

            return requeued;
        }

        public void RemoveProcessingJob(string iid, string queue, string job)
        {
            _redis.RemoveItemFromList(
                String.Format("hangfire:processing:{0}:{1}", iid, queue),
                job);
        }
    }
}
