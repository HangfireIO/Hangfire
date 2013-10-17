using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HangFire.Server
{
    internal class PrioritizedJobFetcher : IJobFetcher
    {
        private readonly List<PrefetchJobFetcher> _fetchers
            = new List<PrefetchJobFetcher>();

        public PrioritizedJobFetcher(
            IEnumerable<string> queues, int prefetchCount)
        {
            foreach (var queue in queues)
            {
                _fetchers.Add(new PrefetchJobFetcher(
                                  new JobFetcher(queue), prefetchCount));
            }
        }

        public void Dispose()
        {
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