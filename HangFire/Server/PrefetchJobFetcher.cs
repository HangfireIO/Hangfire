using System;
using System.Collections.Generic;
using System.Threading;
using HangFire.States;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class PrefetchJobFetcher : IJobFetcher
    {
        private readonly IJobFetcher _innerFetcher;
        private readonly int _count;

        private readonly Queue<JobPayload> _prefetchedItems
            = new Queue<JobPayload>();

        private readonly Thread _prefetchThread;
        private readonly SemaphoreSlim _jobIsReady
            = new SemaphoreSlim(0);
        private CancellationTokenSource _cts
            = new CancellationTokenSource();

        private readonly ILog _logger = LogManager.GetLogger(typeof (PrefetchJobFetcher));

        public PrefetchJobFetcher(
            IJobFetcher innerFetcher, int count)
        {
            _innerFetcher = innerFetcher;
            _count = count;

            _prefetchThread = new Thread(Prefetch)
                {
                    Name = String.Format("HangFire.Prefetch.{0}", "queue"),
                    IsBackground = true
                };
            _prefetchThread.Start();
        }

        public string Queue
        {
            get { return _innerFetcher.Queue; }
        }

        public IRedisClient Redis
        {
            get { return _innerFetcher.Redis; }
        }

        public JobPayload DequeueJob(CancellationToken cancellationToken)
        {
            _jobIsReady.Wait(cancellationToken);

            lock (_prefetchedItems)
            {
                var payload = _prefetchedItems.Dequeue();
                Monitor.Pulse(_prefetchedItems);

                return payload;
            }
        }

        public void Dispose()
        {
            if (_cts != null)
            {
                _cts.Cancel();

                lock (_prefetchedItems)
                {
                    Monitor.Pulse(_prefetchedItems);
                }

                _prefetchThread.Join();

                RequeuePrefetched();

                _innerFetcher.Dispose();

                _jobIsReady.Dispose();
                _cts.Dispose();
                _cts = null;
            }
        }

        private void RequeuePrefetched()
        {
            try
            {
                foreach (var payload in _prefetchedItems)
                {
                    JobState.Apply(
                        Redis,
                        new EnqueuedState(payload.Id, "Re-queue prefetched job.", Queue));

                    JobFetcher.RemoveFromFetchedQueue(Redis, payload.Id, Queue);
                }

                _prefetchedItems.Clear();
            }
            catch (Exception ex)
            {
                _logger.Error("An exception occured while trying to re-queue prefetched jobs. Some prefetched jobs may remain in the dequeue list.", ex);
            }
        }

        private void Prefetch()
        {
            try
            {
                while (true)
                {
                    lock (_prefetchedItems)
                    {
                        while (_prefetchedItems.Count >= _count && !_cts.Token.IsCancellationRequested)
                        {
                            Monitor.Wait(_prefetchedItems);
                        }

                        var payload = _innerFetcher.DequeueJob(_cts.Token);
                        _prefetchedItems.Enqueue(payload);

                        _jobIsReady.Release();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.Fatal("Unexpected exception caught. Jobs will not be fetched.", ex);
            }
        }
    }
}