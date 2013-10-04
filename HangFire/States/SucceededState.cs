using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class SucceededState : JobState
    {
        private readonly TimeSpan _jobExpirationTimeout = TimeSpan.FromDays(1);

        public static readonly string Name = "Succeeded";

        public SucceededState(string jobId, string reason) 
            : base(jobId, reason)
        {
        }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties()
        {
            return new Dictionary<string, string>
                {
                    { "SucceededAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) }
                };
        }

        public override void Apply(IRedisTransaction transaction)
        {
            transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format("hangfire:job:{0}", JobId),
                _jobExpirationTimeout));

            transaction.QueueCommand(x => x.ExpireEntryIn(
                String.Format("hangfire:job:{0}:history", JobId),
                _jobExpirationTimeout));

            transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:succeeded"));
        }

        public class Descriptor : JobState.Descriptor
        {
            public override void Unapply(IRedisTransaction transaction, string jobId)
            {
                transaction.QueueCommand(x => x.DecrementValue("hangfire:stats:succeeded"));
                transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                    String.Format("hangfire:job:{0}", jobId)));
                transaction.QueueCommand(x => ((IRedisNativeClient)x).Persist(
                    String.Format("hangfire:job:{0}:history", jobId)));
            }

            public override IList<string> GetPropertyKeys()
            {
                return new List<string> { "SucceededAt" };
            }
        }
    }
}
