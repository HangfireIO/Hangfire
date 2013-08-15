using System;

namespace HangFire
{
    public class WorkerActivator
    {
        public Worker CreateWorker(Type workerType)
        {
            return (Worker)Activator.CreateInstance(workerType);
        }
    }
}
