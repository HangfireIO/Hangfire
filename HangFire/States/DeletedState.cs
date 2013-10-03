using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class DeletedState : JobState
    {
        public static readonly string Name = "Deleted";

        public DeletedState(string jobId, string reason) 
            : base(jobId, reason)
        {
        }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties()
        {
            throw new NotImplementedException();
        }

        public override void Apply(IRedisTransaction transaction)
        {
            throw new NotImplementedException();
        }
    }
}
