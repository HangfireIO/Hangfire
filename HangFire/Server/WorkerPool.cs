using System;

namespace HangFire.Server
{
    public class WorkerPool
    {
        public WorkerPool()
            : this(Environment.ProcessorCount)
        {
        }

        public WorkerPool(int workersCount)
            : this(workersCount, "default") // TODO: use const
        {
        }

        public WorkerPool(int workersCount, string queue)
        {
            WorkersCount = workersCount;
            Queue = queue;
        }

        public int WorkersCount { get; private set; }
        public string Queue { get; private set; }
    }
}
