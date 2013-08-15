using System;

namespace HangFire
{
    public class WorkerActivator
    {
        public virtual Worker CreateWorker(Type workerType)
        {
            return (Worker)Activator.CreateInstance(workerType);
        }
    }
}
