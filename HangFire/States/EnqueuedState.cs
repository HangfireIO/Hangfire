using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class EnqueuedState : JobState
    {
        public static readonly string Name = "Enqueued";

        public EnqueuedState(string jobId, string reason, string queue) 
            : base(jobId, reason)
        {
            Queue = queue;
        }

        public string Queue { get; private set; }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties()
        {
            return new Dictionary<string, string>
                {
                    { "EnqueuedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                    { "Queue", Queue }
                };
        }

        public override void Apply(IRedisTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            transaction.QueueCommand(x => x.AddItemToSet("hangfire:queues", Queue));
            transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format("hangfire:queue:{0}", Queue), JobId));
        }
    }
}
