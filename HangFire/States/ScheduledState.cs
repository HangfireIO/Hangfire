using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class ScheduledState : JobState
    {
        public static readonly string Name = "Scheduled";

        public ScheduledState(string jobId, string reason, DateTime enqueueAt)
            : base(jobId, reason)
        {
            EnqueueAt = enqueueAt;
        }

        public DateTime EnqueueAt { get; private set; }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties()
        {
            return new Dictionary<string, string>
                {
                    { "ScheduledAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                    { "EnqueueAt", JobHelper.ToStringTimestamp(EnqueueAt) }
                };
        }

        public override void Apply(IRedisTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            var timestamp = JobHelper.ToTimestamp(EnqueueAt);

            transaction.QueueCommand(x => x.AddItemToSortedSet(
                "hangfire:schedule", JobId, timestamp));
        }
    }
}
