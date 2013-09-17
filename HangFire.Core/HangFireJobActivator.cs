using System;

namespace HangFire
{
    public class HangFireJobActivator
    {
        public virtual BackgroundJob ActivateJob(Type jobType)
        {
            return (BackgroundJob)Activator.CreateInstance(jobType);
        }
    }
}
