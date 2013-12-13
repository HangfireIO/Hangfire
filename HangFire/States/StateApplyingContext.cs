using System;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class StateApplyingContext : StateContext
    {
        internal StateApplyingContext(
            StateContext context, IRedisTransaction transaction)
            : base(context)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            Transaction = transaction;
        }

        public IRedisTransaction Transaction { get; private set; }
    }
}