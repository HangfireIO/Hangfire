using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class FailedStateArgs : JobStateArgs
    {
        public FailedStateArgs(string jobId, Exception exception) 
            : base(jobId)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }

    internal class FailedState : JobState<FailedStateArgs>
    {
        public override string StateName
        {
            get { return "Failed"; }
        }

        protected override void ApplyCore(IRedisTransaction transaction, FailedStateArgs args)
        {
            transaction.QueueCommand(x => x.AddItemToSortedSet(
                        "hangfire:failed",
                        args.JobId,
                        JobHelper.ToTimestamp(DateTime.UtcNow)));

            transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:failed"));
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

        protected override void UnapplyCore(IRedisTransaction transaction, string jobId)
        {
            transaction.QueueCommand(x => x.DecrementValue("hangfire:stats:failed"));
            transaction.QueueCommand(x => x.RemoveItemFromSortedSet("hangfire:failed", jobId));
        }

        protected override IDictionary<string, string> GetProperties(FailedStateArgs args)
        {
            return new Dictionary<string, string>
                {
                    { "FailedAt", JobHelper.ToJson(DateTime.UtcNow) },
                    { "ExceptionType", args.Exception.GetType().FullName },
                    { "ExceptionMessage", args.Exception.Message },
                    { "ExceptionDetails", args.Exception.ToString() }
                };
        }
    }
}
