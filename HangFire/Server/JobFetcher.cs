using System;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobFetcher
    {
        private readonly TimeSpan _fetchTimeout;

        private readonly IRedisClient _redis;
        private readonly string _queue;

        public JobFetcher(IRedisClient redis, string queue, TimeSpan? fetchTimeout = null)
        {
            _redis = redis;
            _queue = queue;

            _fetchTimeout = fetchTimeout ?? TimeSpan.FromSeconds(5);
        }

        public string DequeueJobId()
        {
            var jobId = _redis.BlockingPopAndPushItemBetweenLists(
                    String.Format("hangfire:queue:{0}", _queue),
                    String.Format("hangfire:queue:{0}:dequeued", _queue),
                    _fetchTimeout);

            // Checkpoint #1. 

            // Fail point N1. The job has no fetched flag set.

            if (!String.IsNullOrEmpty(jobId))
            {
                _redis.SetEntry(
                    String.Format("hangfire:job:{0}:fetched", jobId),
                    JobHelper.ToStringTimestamp(DateTime.UtcNow));
            }

            // Fail point N2. N

            return jobId;
        }
    }
}
