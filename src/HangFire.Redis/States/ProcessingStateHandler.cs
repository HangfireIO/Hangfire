using System;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;

namespace HangFire.Redis.States
{
    internal class ProcessingStateHandler : JobStateHandler
    {
        public override void Apply(
            StateApplyingContext context, IWriteOnlyTransaction transaction)
        {
            transaction.AddToSet(
                "processing",
                context.JobId,
                JobHelper.ToTimestamp(DateTime.UtcNow));
        }

        public override void Unapply(
            StateApplyingContext context, IWriteOnlyTransaction transaction)
        {
            transaction.RemoveFromSet("processing", context.JobId);
        }

        public override string StateName
        {
            get { return ProcessingState.Name; }
        }
    }
}
