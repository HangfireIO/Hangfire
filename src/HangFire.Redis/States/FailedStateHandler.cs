using System;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;

namespace HangFire.Redis.States
{
    internal class FailedStateHandler : StateHandler
    {
        public override void Apply(
            ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            transaction.AddToSet(
                "failed",
                context.JobId,
                JobHelper.ToTimestamp(DateTime.UtcNow));
        }

        public override void Unapply(
            ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            transaction.RemoveFromSet("failed", context.JobId);
        }

        public override string StateName
        {
            get { return FailedState.StateName; }
        }
    }
}
