using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class ProcessingState : JobState
    {
        public static readonly string Name = "Processing";

        public ProcessingState(string jobId, string serverName) 
            : base(jobId)
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
            transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:processing"));
            transaction.QueueCommand(x => x.AddItemToSet(
                "hangfire:processing", JobId));
        }

        public static void Unapply(IRedisTransaction transaction, string jobId)
        {
            transaction.QueueCommand(x => x.DecrementValue("hangfire:stats:processing"));
            transaction.QueueCommand(x => x.RemoveItemFromSet(
                "hangfire:processing", jobId));
        }
    }
}
