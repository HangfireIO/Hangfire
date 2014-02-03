using System;
using HangFire.Server;
using ServiceStack.Redis;

namespace HangFire.Storage
{
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