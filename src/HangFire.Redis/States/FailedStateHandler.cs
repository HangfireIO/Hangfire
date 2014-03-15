using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire.Redis.States
{
    internal class FailedStateHandler : JobStateHandler
    {
        public override void Apply(StateApplyingContext context, IDictionary<string, string> stateData)
        {
            context.Transaction.AddToSet(
                "failed",
                context.JobId,
                JobHelper.ToTimestamp(DateTime.UtcNow));
        }

        public override void Unapply(StateApplyingContext context)
        {
            context.Transaction.RemoveFromSet("failed", context.JobId);
        }

        public override string StateName
        {
            get { return FailedState.Name; }
        }
    }
}
