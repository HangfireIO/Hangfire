using System;

namespace HangFire
{
    public class JobActivator
    {
        public virtual BackgroundJob ActivateJob(Type jobType)
        {
            return (BackgroundJob)Activator.CreateInstance(jobType);
        }
    }
}
