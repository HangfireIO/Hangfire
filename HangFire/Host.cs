using System;

namespace HangFire
{
    public class Host
    {
        private readonly JobFetcher _fetcher;
        private readonly JobDispatcherPool _pool;

        public Host(int concurrency)
        {
            _pool = new JobDispatcherPool(concurrency);
            _pool.JobCompleted += PoolOnJobCompleted;

            _fetcher = new JobFetcher();
            _fetcher.JobFetched += FetcherOnJobFetched;
        }

        public void Start()
        {
            _fetcher.Start();
        }

        private void PoolOnJobCompleted(object sender, Tuple<string, Exception> tuple)
        {
            _fetcher.Process(tuple);
        }

        private void FetcherOnJobFetched(object sender, string serializedJob)
        {
            _pool.Process(serializedJob);
        }
    }
}