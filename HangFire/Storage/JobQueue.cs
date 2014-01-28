using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using HangFire.Common;
using HangFire.Server;
using ServiceStack.Redis;

namespace HangFire.Storage
{
    /// <summary>
    /// Represents a job queue.
    /// </summary>
    internal class JobQueue
    {
        private readonly IRedisClient _redis;
        private readonly string _queue;
        private readonly TimeSpan _fetchTimeout;

        public JobQueue(IRedisClient redis, string queue, TimeSpan fetchTimeout)
        {
            _redis = redis;
            _queue = queue;
            _fetchTimeout = fetchTimeout;
        }

        public QueuedJob TakeNext(CancellationToken cancellationToken)
        {
            string jobId;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                jobId = _redis.BlockingPopAndPushItemBetweenLists(
                    String.Format("hangfire:queue:{0}", _queue),
                    String.Format("hangfire:queue:{0}:dequeued", _queue),
                    _fetchTimeout);
            } while (jobId == null);

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

            Dictionary<string, string> job = null;

            using (var pipeline = _redis.CreatePipeline())
            {
                pipeline.QueueCommand(x => x.SetEntryInHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Fetched",
                    JobHelper.ToStringTimestamp(DateTime.UtcNow)));

                // ServiceStack.Redis library could not queue a command,
                // that returns IDictionary, so, let's build it using MGET.
                pipeline.QueueCommand(
                    x => x.GetValuesFromHash(
                        String.Format("hangfire:job:{0}", jobId),
                        new[] { "Type", "Args", "Method", "Arguments", "ParameterTypes" }),
                    x => job = new Dictionary<string, string>
                    {
                        { "Type", x[0] },
                        { "Args", x[1] },
                        { "Method", x[2] },
                        { "Arguments", x[3] },
                        { "ParameterTypes", x[4] }
                    });

                pipeline.Flush();
            }

            // Checkpoint #2. The job is in the implicit 'Fetched' state now.
            // This state stores information about fetched time. The job will
            // be re-queued when the JobTimeout will be expired.

            return new QueuedJob(new JobPayload(jobId, _queue, job));
        }
    }

    internal class QueuedJob
    {
        public QueuedJob(JobPayload payload)
        {
            Payload = payload;
        }

        public JobPayload Payload { get; private set; }

        public void Complete(IRedisClient redis, bool canceled = false)
        {
            Remove(redis, Payload.Queue, Payload.Id);
        }

        internal static void Remove(IRedisClient redis, string queue, string jobId)
        {
            using (var transaction = redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromList(
                    String.Format("hangfire:queue:{0}:dequeued", queue),
                    jobId,
                    -1));

                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Fetched"));
                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Checked"));

                transaction.Commit();
            }
        }
    }
}
