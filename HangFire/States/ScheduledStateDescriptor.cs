using ServiceStack.Redis;

namespace HangFire.States
{
    public class ScheduledStateDescriptor : JobStateDescriptor
    {
        public override void Unapply(IRedisTransaction transaction, string jobId)
        {
            transaction.QueueCommand(x => x.RemoveItemFromSortedSet("hangfire:schedule", jobId));
        }
    }
}