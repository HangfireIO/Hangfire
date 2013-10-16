using System;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobFetcher
    {
        private readonly IRedisClient _redis;
        private readonly string _queue;
        private readonly TimeSpan _fetchTimeout;

        private readonly ILog _logger = LogManager.GetLogger(typeof (JobFetcher));

        public JobFetcher(
            IRedisClient redis, string queue, TimeSpan? fetchTimeout = null)
        {
            _redis = redis;
            _queue = queue;

            _fetchTimeout = fetchTimeout ?? TimeSpan.FromSeconds(5);
        }

        public JobPayload DequeueJob()
        {
            var jobId = _redis.BlockingPopAndPushItemBetweenLists(
                    String.Format("hangfire:queue:{0}", _queue),
                    String.Format("hangfire:queue:{0}:dequeued", _queue),
                    _fetchTimeout);
            
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

            string jobArgs = null;
            string jobType = null;

            using (var pipeline = _redis.CreatePipeline())
            {
                pipeline.QueueCommand(x => x.SetEntry(
                    String.Format("hangfire:job:{0}:fetched", jobId),
                    JobHelper.ToStringTimestamp(DateTime.UtcNow)));

                pipeline.QueueCommand(
                    x => x.GetValuesFromHash(
                        String.Format("hangfire:job:{0}", jobId),
                        new[] { "Type", "Args" }),
                    x => { jobType = x[0]; jobArgs = x[1]; });

                pipeline.Flush();
            }

            // Checkpoint #2. The job is in the implicit 'Fetched' state now.
            // This state stores information about fetched time. The job will
            // be re-queued when the JobTimeout will be expired.

            if (String.IsNullOrEmpty(jobType))
            {
                _logger.Warn(String.Format(
                    "Could not process the job '{0}': it does not exist in the storage.",
                    jobId));

                return null;
            }

            return new JobPayload(jobId, _queue, jobType, jobArgs);
        }

        public static void RemoveFromFetchedQueue(
            IRedisClient redis, string jobId, string queue)
        {
            using (var transaction = redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromList(
                    String.Format("hangfire:queue:{0}:dequeued", queue),
                    jobId,
                    -1));

                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:job:{0}:fetched", jobId),
                    String.Format("hangfire:job:{0}:checked", jobId)));

                transaction.Commit();
            }
        }
    }
}
