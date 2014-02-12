using HangFire.Server;

namespace HangFire.Storage
{
    public class QueuedJob
    {
        public QueuedJob(JobPayload payload)
        {
            Payload = payload;
        }

        public JobPayload Payload { get; private set; }

        public void Complete(IStorageConnection connection)
        {
            connection.Jobs.Complete(Payload);
        }
    }
}