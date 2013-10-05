using System;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class SucceededStateDescriptor : JobStateDescriptor
    {
        public override void Unapply(IRedisTransaction transaction, string jobId)
        {
            transaction.QueueCommand(x => x.DecrementValue("hangfire:stats:succeeded"));
            transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format("hangfire:job:{0}", jobId)));
            transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                String.Format("hangfire:job:{0}:history", jobId)));
        }
    }
}