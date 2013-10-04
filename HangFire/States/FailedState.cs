using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class FailedState : JobState
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
        }

        public class Descriptor : JobState.Descriptor
        {
            public override void Unapply(IRedisTransaction transaction, string jobId)
            {
                transaction.QueueCommand(x => x.RemoveItemFromSortedSet("hangfire:failed", jobId));
            }
        }
    }
}
