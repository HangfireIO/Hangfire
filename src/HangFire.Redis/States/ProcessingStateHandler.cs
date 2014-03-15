using System;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire.Redis.States
{
    internal class ProcessingStateHandler : JobStateHandler
    {
        public override void Apply(StateApplyingContext context)
        {
            context.Transaction.AddToSet(
                "processing",
                context.JobId,
                JobHelper.ToTimestamp(DateTime.UtcNow));
        }

        public override void Unapply(StateApplyingContext context)
        {
            context.Transaction.RemoveFromSet("processing", context.JobId);
        }

        public override string StateName
        {
            get { return ProcessingState.Name; }
        }
    }
}
