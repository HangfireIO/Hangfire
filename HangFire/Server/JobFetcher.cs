using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobFetcher
    {
        private readonly TimeSpan _fetchTimeout;

        private readonly IRedisClient _redis;
        private readonly IList<string> _queues;
        private int _currentQueueIndex = 0;

        public JobFetcher(
            IRedisClient redis, IList<string> queues, TimeSpan? fetchTimeout = null)
        {
            _redis = redis;
            _queues = queues;

            _fetchTimeout = fetchTimeout ?? TimeSpan.FromSeconds(5);
        }

        public string DequeueJobId()
        {
            var jobId = _redis.BlockingPopAndPushItemBetweenLists(
                    String.Format("hangfire:queue:{0}", _queues[_currentQueueIndex]),
                    String.Format("hangfire:queue:{0}:dequeued", _queues[_currentQueueIndex]),
                    _fetchTimeout);

            _currentQueueIndex = (_currentQueueIndex + 1) % _queues.Count;

            if (String.IsNullOrEmpty(jobId))
            {
                return null;
            }

            // The job was dequeued by the server. To provide reliability,
            // we should ensure, that the job will be performed and acquired
            // resources will be disposed even if the server will crash 
            // while executing one of the subsequent lines of code.

            // The job's processing is splitted into a couple of checkpoints.
            // Each checkpoint occurs after successful update of the 
            // job information in the storage. And each checkpoint describes
            // the way to perform the job when the server was crashed after
            // reaching it.

            // Checkpoint #1-1. The job was dequeued into the dequeued list,
            // that is being inspected by the DequeuedJobsWatcher instance.
            // Job's has the implicit 'Dequeued' state.

            _redis.SetEntry(
                String.Format("hangfire:job:{0}:fetched", jobId),
                JobHelper.ToStringTimestamp(DateTime.UtcNow));

            // Checkpoint #2. The job is in the implicit 'Fetched' state now.
            // This state stores information about fetched time. The job will
            // be re-queued when the JobTimeout will be expired.

            return jobId;
        }
    }
}
