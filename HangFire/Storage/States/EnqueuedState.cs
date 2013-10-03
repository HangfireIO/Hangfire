using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class EnqueuedStateArgs : JobStateArgs
    {
        public EnqueuedStateArgs(string jobId, string queueName) 
            : base(jobId)
        {
            QueueName = queueName;
        }

        public string QueueName { get; private set; }
    }

    internal class EnqueuedState : JobState<EnqueuedStateArgs>
    {
        protected override void ApplyCore(IRedisTransaction transaction, EnqueuedStateArgs args)
        {
            transaction.QueueCommand(x => x.AddItemToSet("hangfire:queues", args.QueueName));
            transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format("hangfire:queue:{0}", args.QueueName), args.JobId));
        }

        protected override IDictionary<string, string> GetProperties(EnqueuedStateArgs args)
        {
            return new Dictionary<string, string>
                {
                    { "EnqueuedAt", JobHelper.ToJson(DateTime.UtcNow) }
                };
        }

        public override string StateName
        {
            get { return "Enqueued"; }
        }

        protected override void UnapplyCore(IRedisTransaction transaction, string jobId)
        {
        }
    }
}
