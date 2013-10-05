using System;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class ProcessingStateDescriptor : JobStateDescriptor
    {
        public override void Unapply(IRedisTransaction transaction, string jobId)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            transaction.QueueCommand(x => x.RemoveItemFromSet(
                "hangfire:processing", jobId));
        }
    }
}