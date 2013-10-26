using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class FailedState : JobState
    {
        public static readonly string Name = "Failed";

        public FailedState(string reason, Exception exception) 
            : base(reason)
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

        public override void Apply(IRedisTransaction transaction, string jobId)
        {
            transaction.QueueCommand(x => x.AddItemToSortedSet(
                        "hangfire:failed",
                        jobId,
                        JobHelper.ToTimestamp(DateTime.UtcNow)));
        }

        public class Descriptor : JobStateDescriptor
        {
            public override void Unapply(IRedisTransaction transaction, string jobId)
            {
                transaction.QueueCommand(x => x.RemoveItemFromSortedSet("hangfire:failed", jobId));
            }
        }
    }
}
