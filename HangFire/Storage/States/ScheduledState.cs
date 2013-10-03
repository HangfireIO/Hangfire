using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class ScheduledStateArgs : JobStateArgs
    {
        public ScheduledStateArgs(string jobId, string queueName, DateTime fireAt)
            : base(jobId)
        {
            QueueName = queueName;
            FireAt = fireAt;
        }

        public string QueueName { get; private set; }
        public DateTime FireAt { get; private set; }
    }

    internal class ScheduledState : JobState<ScheduledStateArgs>
    {
        public override string StateName
        {
            get { return "Scheduled"; }
        }

        protected override void ApplyCore(IRedisTransaction transaction, ScheduledStateArgs args)
        {
            var timestamp = DateTimeToTimestamp(args.FireAt);

            transaction.QueueCommand(x => x.AddItemToSortedSet(
                "hangfire:schedule", args.JobId, timestamp));
        }

        protected override void UnapplyCore(IRedisTransaction transaction, string jobId)
        {
            transaction.QueueCommand(x => x.RemoveItemFromSortedSet("hangfire:schedule", jobId));
        }

        protected override IDictionary<string, string> GetProperties(ScheduledStateArgs args)
        {
            return new Dictionary<string, string>
                {
                    { "ScheduledAt", JobHelper.ToJson(DateTime.UtcNow) },
                    { "ScheduledQueue", args.QueueName }
                };
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long DateTimeToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }
    }
}
