using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class SucceededState : JobState
    {
        private readonly TimeSpan _jobExpirationTimeout = TimeSpan.FromDays(1);

        public static readonly string Name = "Succeeded";

        public SucceededState(string reason)
            : base(reason)
        {
        }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties(JobDescriptor descriptor)
        {
            return new Dictionary<string, string>
                {
                    { "SucceededAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) }
                };
        }

        public override void Apply(JobDescriptor descriptor, IRedisTransaction transaction)
        {
            transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format("hangfire:job:{0}", descriptor.JobId),
                _jobExpirationTimeout));

            transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format("hangfire:job:{0}:history", descriptor.JobId),
                _jobExpirationTimeout));

            transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format("hangfire:job:{0}:state", descriptor.JobId),
                _jobExpirationTimeout));

            transaction.QueueCommand(x => x.EnqueueItemOnList("hangfire:succeeded", descriptor.JobId));
            transaction.QueueCommand(x => x.TrimList("hangfire:succeeded", 0, 99));

            transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:succeeded"));
        }

        public class Descriptor : JobStateDescriptor
        {
            public override void Unapply(JobDescriptor descriptor, IRedisTransaction transaction)
            {
                transaction.QueueCommand(x => x.DecrementValue("hangfire:stats:succeeded"));

                transaction.QueueCommand(x => x.RemoveItemFromList(
                    "hangfire:succeeded", descriptor.JobId));

                transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                    String.Format("hangfire:job:{0}", descriptor.JobId)));
                transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                    String.Format("hangfire:job:{0}:history", descriptor.JobId)));
                transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                    String.Format("hangfire:job:{0}:state", descriptor.JobId)));
            }
        }
    }
}
