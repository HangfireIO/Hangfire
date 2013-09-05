using System;

namespace HangFire
{
    public class HangFireJobActivator
    {
        public virtual HangFireJob ActivateJob(Type jobType)
        {
            return (HangFireJob)Activator.CreateInstance(jobType);
        }
    }
}
