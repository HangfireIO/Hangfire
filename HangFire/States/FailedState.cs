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

        public override IDictionary<string, string> GetProperties(JobDescriptor descriptor)
        {
            return new Dictionary<string, string>
                {
                    { "FailedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                    { "ExceptionType", Exception.GetType().FullName },
                    { "ExceptionMessage", Exception.Message },
                    { "ExceptionDetails", Exception.ToString() }
                };
        }

        public override void Apply(JobDescriptor descriptor, IRedisTransaction transaction)
        {
            transaction.QueueCommand(x => x.AddItemToSortedSet(
                        "hangfire:failed",
                        descriptor.JobId,
                        JobHelper.ToTimestamp(DateTime.UtcNow)));
        }

        public class Descriptor : JobStateDescriptor
        {
            public override void Unapply(JobDescriptor descriptor, IRedisTransaction transaction)
            {
                transaction.QueueCommand(x => x.RemoveItemFromSortedSet(
                    "hangfire:failed", descriptor.JobId));
            }
        }
    }
}
