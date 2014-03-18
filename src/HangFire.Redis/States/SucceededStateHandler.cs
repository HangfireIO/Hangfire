using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;

namespace HangFire.Redis.States
{
    internal class SucceededStateHandler : StateHandler
    {
        public override void Apply(
            StateApplyingContext context, IWriteOnlyTransaction transaction)
        {
            transaction.InsertToList("succeeded", context.JobId);
            transaction.TrimList("succeeded", 0, 99);
        }

        public override void Unapply(
            StateApplyingContext context, IWriteOnlyTransaction transaction)
        {
            transaction.RemoveFromList("succeeded", context.JobId);
        }

        public override string StateName
        {
            get { return SucceededState.StateName; }
        }
    }
}
