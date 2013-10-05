using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class ProcessingState : JobState
    {
        public static readonly string Name = "Processing";

        public ProcessingState(string jobId, string reason, string serverName) 
            : base(jobId, reason)
        {
            ServerName = serverName;
        }

        public string ServerName { get; private set; }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties()
        {
            return new Dictionary<string, string>
                {
                    { "StartedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                    { "ServerName", ServerName }
                };
        }

        public override void Apply(IRedisTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            transaction.QueueCommand(x => x.AddItemToSet(
                "hangfire:processing", JobId));
        }
    }
}
