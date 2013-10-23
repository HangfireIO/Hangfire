using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class PrioritizedJobFetcher : IJobFetcher
    {
        private readonly List<PrefetchJobFetcher> _fetchers
            = new List<PrefetchJobFetcher>();

        public PrioritizedJobFetcher(
            IRedisClientsManager redisManager,
            IEnumerable<string> queues, int prefetchCount)
        {
            foreach (var queue in queues)
            {
                _fetchers.Add(new PrefetchJobFetcher(
                    new JobFetcher(redisManager, queue), prefetchCount));
            }
        }

        public void Dispose()
        {
            foreach (var fetcher in _fetchers)
            {
                fetcher.SendStop();
            }

            foreach (var fetcher in _fetchers)
            {
                fetcher.Dispose();
            }
        }

        public JobPayload DequeueJob(CancellationToken cancellationToken)
        {
            var waitHandles = _fetchers.Select(x => x.JobIsReady).ToList();
            waitHandles.Add(cancellationToken.WaitHandle);

            WaitHandle.WaitAny(waitHandles.ToArray());

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var fetcher in _fetchers)
            {
                if (fetcher.PrefetchedCount > 0)
                {
                    return fetcher.DequeueJob(cancellationToken);
                }
            }

            throw new InvalidOperationException();
        }
    }
}