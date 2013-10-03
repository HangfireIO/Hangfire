using System;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class DeletedState : JobState<JobStateArgs>
    {
        protected override void ApplyCore(IRedisTransaction transaction, JobStateArgs args)
        {
            throw new NotImplementedException();
        }

        public override string StateName
        {
            get { throw new NotImplementedException(); }
        }

        protected override void UnapplyCore(IRedisTransaction transaction, string jobId)
        {
            throw new NotImplementedException();
        }
    }
}
