using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire.Redis.States
{
    class FailedStateHandler : JobStateHandler
    {
        public override void Apply(StateApplyingContext context, IDictionary<string, string> stateData)
        {
            context.Transaction.Sets.Add(
                "failed",
                context.JobId,
                JobHelper.ToTimestamp(DateTime.UtcNow));
        }

        public override void Unapply(StateApplyingContext context)
        {
            context.Transaction.Sets.Remove("failed", context.JobId);
        }

        public override string StateName
        {
            get { return FailedState.Name; }
        }
    }
}
