using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class EnqueuedState : JobState
    {
        public static readonly string Name = "Enqueued";

        public EnqueuedState(string jobId, string reason, string queueName) 
            : base(jobId, reason)
        {
            QueueName = queueName;
        }

        public string QueueName { get; private set; }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties()
        {
            return new Dictionary<string, string>
                {
                    { "EnqueuedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) }
                };
        }

        public override void Apply(IRedisTransaction transaction)
        {
            transaction.QueueCommand(x => x.AddItemToSet("hangfire:queues", QueueName));
            transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format("hangfire:queue:{0}", QueueName), JobId));
        }
    }
}
