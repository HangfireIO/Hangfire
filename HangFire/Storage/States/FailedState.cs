using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class FailedState : JobState
    {
        public static readonly string Name = "Failed";

        public FailedState(string jobId, string reason, Exception exception) 
            : base(jobId, reason)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties()
        {
            return new Dictionary<string, string>
                {
                    { "FailedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                    { "ExceptionType", Exception.GetType().FullName },
                    { "ExceptionMessage", Exception.Message },
                    { "ExceptionDetails", Exception.ToString() }
                };
        }

        public override void Apply(IRedisTransaction transaction)
        {
            transaction.QueueCommand(x => x.AddItemToSortedSet(
                        "hangfire:failed",
                        JobId,
                        JobHelper.ToTimestamp(DateTime.UtcNow)));

            transaction.QueueCommand(x => x.IncrementValue(
                String.Format("hangfire:stats:failed:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

            transaction.QueueCommand(x => x.IncrementValue(
                String.Format("hangfire:stats:failed:{0}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm"))));

            var hourlyFailedKey = String.Format(
                "hangfire:stats:failed:{0}",
                DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));
            transaction.QueueCommand(x => x.IncrementValue(hourlyFailedKey));
            transaction.QueueCommand(x => x.ExpireEntryIn(hourlyFailedKey, TimeSpan.FromDays(1)));
        }

        public static void Unapply(IRedisTransaction transaction, string jobId)
        {
            transaction.QueueCommand(x => x.RemoveItemFromSortedSet("hangfire:failed", jobId));
        }
    }
}
