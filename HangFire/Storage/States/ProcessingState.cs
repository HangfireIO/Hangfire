using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class ProcessingStateArgs : JobStateArgs
    {
        public ProcessingStateArgs(string jobId, string serverName) 
            : base(jobId)
        {
            ServerName = serverName;
        }

        public string ServerName { get; private set; }
    }

    internal class ProcessingState : JobState<ProcessingStateArgs>
    {
        public override string StateName
        {
            get { return "Processing"; }
        }

        protected override void ApplyCore(IRedisTransaction transaction, ProcessingStateArgs args)
        {
            transaction.QueueCommand(x => x.IncrementValue("hangfire:stats:processing"));
            transaction.QueueCommand(x => x.AddItemToSet(
                "hangfire:processing", args.JobId));
        }

        protected override void UnapplyCore(IRedisTransaction transaction, string jobId)
        {
            transaction.QueueCommand(x => x.DecrementValue("hangfire:stats:processing"));
            transaction.QueueCommand(x => x.RemoveItemFromSet(
                "hangfire:processing", jobId));
        }

        protected override IDictionary<string, string> GetProperties(ProcessingStateArgs args)
        {
            return new Dictionary<string, string>
                {
                    { "StartedAt", JobHelper.ToJson(DateTime.UtcNow) },
                    { "ServerName", args.ServerName }
                };
        }
    }
}
