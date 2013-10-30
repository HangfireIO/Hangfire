using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class ScheduledState : JobState
    {
        public static readonly string Name = "Scheduled";

        public ScheduledState(string reason, DateTime enqueueAt)
            : base(reason)
        {
            EnqueueAt = enqueueAt;
        }

        public DateTime EnqueueAt { get; private set; }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties(JobDescriptor descriptor)
        {
            return new Dictionary<string, string>
                {
                    { "ScheduledAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                    { "EnqueueAt", JobHelper.ToStringTimestamp(EnqueueAt) }
                };
        }

        public override void Apply(JobDescriptor descriptor, IRedisTransaction transaction)
        {
            var timestamp = JobHelper.ToTimestamp(EnqueueAt);

            transaction.QueueCommand(x => x.AddItemToSortedSet(
                "hangfire:schedule", descriptor.JobId, timestamp));
        }

        public class Descriptor : JobStateDescriptor
        {
            public override void Unapply(JobDescriptor descriptor, IRedisTransaction transaction)
            {
                transaction.QueueCommand(x => x.RemoveItemFromSortedSet(
                    "hangfire:schedule", descriptor.JobId));
            }
        }
    }
}
