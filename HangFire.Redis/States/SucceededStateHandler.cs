using System.Collections.Generic;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire.Redis.States
{
    internal class SucceededStateHandler : JobStateHandler
    {
        public override void Apply(StateApplyingContext context, IDictionary<string, string> stateData)
        {
            context.Transaction.Lists.AddToLeft("succeeded", context.JobId);
            context.Transaction.Lists.Trim("succeeded", 0, 99);
        }

        public override void Unapply(StateApplyingContext context)
        {
            context.Transaction.Lists.Remove("succeeded", context.JobId);
        }

        public override string StateName
        {
            get { return SucceededState.Name; }
        }
    }
}
