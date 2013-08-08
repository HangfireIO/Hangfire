using System.Threading;
using ServiceStack.Logging;

namespace HangFire
{
    public class Manager
    {
        private readonly JobDispatcherPool _pool;
        private Thread _balancerThread;

        public Manager(int concurrency)
        {
            _pool = new JobDispatcherPool(concurrency);
        }

        public void Start()
        {
            _balancerThread = new Thread(BalanceTasks) { IsBackground = true };
            _balancerThread.Start();
        }

        private void BalanceTasks()
        {
            // TODO: handle connection exceptions.
            using (var redis = Factory.CreateRedisClient())
            {
                while (true)
                {
                    var serializedJob = redis.BlockingDequeueItemFromList("queue:default", null);
                    _pool.Process(serializedJob);
                }
            }
        }
    }
}
