using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class SucceededState : JobState
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
            transaction.QueueCommand(x => x.IncrementValue(
                String.Format("hangfire:stats:succeeded:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

            transaction.QueueCommand(x => x.EnqueueItemOnList("hangfire:succeeded", JobId));
            transaction.QueueCommand(x => x.TrimList("hangfire:succeeded", 0, 99));

            var hourlySucceededKey = String.Format(
                "hangfire:stats:succeeded:{0}",
                DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));
            transaction.QueueCommand(x => x.IncrementValue(hourlySucceededKey));
            transaction.QueueCommand(x => x.ExpireEntryIn(hourlySucceededKey, TimeSpan.FromDays(1)));
        }

        public static void Unapply(IRedisTransaction transaction, string jobId)
        {
            // TODO: persist the job
        }
    }
}
