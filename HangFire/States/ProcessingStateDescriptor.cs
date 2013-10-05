using ServiceStack.Redis;

namespace HangFire.States
{
    public class ProcessingStateDescriptor : JobStateDescriptor
    {
        public override void Unapply(IRedisTransaction transaction, string jobId)
        {
            transaction.QueueCommand(x => x.RemoveItemFromSet(
                "hangfire:processing", jobId));
        }
    }
}