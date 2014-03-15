using System.Collections.Generic;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire.Redis.States
{
    internal class SucceededStateHandler : JobStateHandler
    {
        public override void Apply(StateApplyingContext context, IDictionary<string, string> stateData)
        {
            context.Transaction.InsertToList("succeeded", context.JobId);
            context.Transaction.TrimList("succeeded", 0, 99);
        }

        public override void Unapply(StateApplyingContext context)
        {
            context.Transaction.RemoveFromList("succeeded", context.JobId);
        }

        public override string StateName
        {
            get { return SucceededState.Name; }
        }
    }
}
