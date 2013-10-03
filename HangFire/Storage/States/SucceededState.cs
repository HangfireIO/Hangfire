using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class SucceededState : JobState<JobStateArgs>
    {
        private readonly TimeSpan _jobExpirationTimeout = TimeSpan.FromDays(1);

        protected override void ApplyCore(IRedisTransaction transaction, JobStateArgs args)
        {
            transaction.QueueCommand(x => x.ExpireEntryIn(
                        String.Format("hangfire:job:{0}", args.JobId),
                        _jobExpirationTimeout));

            transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:succeeded"));
            transaction.QueueCommand(x => x.IncrementValue(
                String.Format("hangfire:stats:succeeded:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

            transaction.QueueCommand(x => x.EnqueueItemOnList("hangfire:succeeded", args.JobId));
            transaction.QueueCommand(x => x.TrimList("hangfire:succeeded", 0, 99));

            var hourlySucceededKey = String.Format(
                "hangfire:stats:succeeded:{0}",
                DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));
            transaction.QueueCommand(x => x.IncrementValue(hourlySucceededKey));
            transaction.QueueCommand(x => x.ExpireEntryIn(hourlySucceededKey, TimeSpan.FromDays(1)));
        }

        protected override IDictionary<string, string> GetProperties(JobStateArgs args)
        {
            return new Dictionary<string, string>
                {
                    { "SucceededAt", JobHelper.ToJson(DateTime.UtcNow) }
                };
        }

        public override string StateName
        {
            get { return "Succeeded"; }
        }

        protected override void UnapplyCore(IRedisTransaction transaction, string jobId)
        {
        }
    }
}
