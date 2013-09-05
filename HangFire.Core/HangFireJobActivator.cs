using System;

namespace HangFire
{
    public class HangFireJobActivator
    {
        public virtual object ActivateJob(Type jobType)
        {
            return Activator.CreateInstance(jobType);
        }
    }
}
