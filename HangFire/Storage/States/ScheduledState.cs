using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class ScheduledState : JobState
    {
        public static readonly string Name = "Scheduled";

        public ScheduledState(string jobId, string queueName, DateTime fireAt)
            : base(jobId)
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
                    { "ScheduledAt", JobHelper.ToJson(DateTime.UtcNow) },
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
