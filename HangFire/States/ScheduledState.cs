using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class ScheduledState : JobState
    {
        public static readonly string Name = "Scheduled";

        public ScheduledState(string jobId, string reason, string queueName, DateTime fireAt)
            : base(jobId, reason)
        {
            QueueName = queueName;
            FireAt = fireAt;
        }

        public string QueueName { get; private set; }
        public DateTime FireAt { get; private set; }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties()
        {
            return new Dictionary<string, string>
                {
                    { "ScheduledAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                    { "ScheduledQueue", QueueName }
                };
        }

        public override void Apply(IRedisTransaction transaction)
        {
            var timestamp = JobHelper.ToTimestamp(FireAt);

            transaction.QueueCommand(x => x.AddItemToSortedSet(
                "hangfire:schedule", JobId, timestamp));
        }

        public static void Unapply(IRedisTransaction transaction, string jobId)
        {
            transaction.QueueCommand(x => x.RemoveItemFromSortedSet("hangfire:schedule", jobId));
        }
    }
}
