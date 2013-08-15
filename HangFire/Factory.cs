using System;

namespace HangFire
{
    internal static class Factory
    {
        public static Worker CreateWorker(Type workerType)
        {
            return (Worker)Activator.CreateInstance(workerType);
        }
    }
}
