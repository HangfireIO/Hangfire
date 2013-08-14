using System;
using System.Threading;

namespace HangFire
{
    public class Host
    {
        private readonly Thread _managerThread;
        private readonly JobFetcher _fetcher;
        private readonly JobDispatcherPool _pool;

        public Host(int concurrency)
        {
            _managerThread = new Thread(Work)
                {
                    Name = "HangFire.Manager", 
                    IsBackground = true
                };

            _pool = new JobDispatcherPool(concurrency);
            _pool.JobFailed += PoolOnJobFailed;

            _fetcher = new JobFetcher();
        }

        private void Work()
        {
            while (true)
            {
                // TODO: handle exceptions
                var dispatcher = _pool.TakeFree();
                var job = _fetcher.TakeNext();

                dispatcher.Process(job);
            }
        }

        public void Start()
        {
            _managerThread.Start();
        }

        private void PoolOnJobFailed(object sender, Tuple<string, Exception> tuple)
        {
            _fetcher.AddToFailedQueue(tuple.Item1);
        }
    }
}