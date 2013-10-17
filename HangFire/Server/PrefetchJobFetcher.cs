using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HangFire.States;
using ServiceStack.Logging;

namespace HangFire.Server
{
    internal class PrefetchJobFetcher : IJobFetcher
    {
        private readonly JobFetcher _innerFetcher;
        private readonly int _count;

        private readonly BlockingCollection<JobPayload> _items
            = new BlockingCollection<JobPayload>(new ConcurrentQueue<JobPayload>());

        private readonly Thread _prefetchThread;
        private readonly ManualResetEventSlim _jobIsReady
            = new ManualResetEventSlim(false);
        private CancellationTokenSource _cts
            = new CancellationTokenSource();

        private readonly ILog _logger = LogManager.GetLogger(typeof (PrefetchJobFetcher));

        public PrefetchJobFetcher(JobFetcher innerFetcher, int count)
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

        public int PrefetchedCount
        {
            get { return _items.Count; }
        }

        public WaitHandle JobIsReady
        {
            get { return _jobIsReady.WaitHandle; }
        }

        public JobPayload DequeueJob(CancellationToken cancellationToken)
        {
            var payload = _items.Take(cancellationToken);

            lock (_items)
            {
                if (_items.Count == 0)
                {
                    _jobIsReady.Reset();
                }

                Monitor.Pulse(_items);
            }

            return payload;
        }

        public void Dispose()
        {
            if (_cts != null)
            {
                _cts.Cancel();

                lock (_items)
                {
                    Monitor.Pulse(_items);
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
                foreach (var payload in _items)
                {
                    JobState.Apply(
                        _innerFetcher.Redis,
                        new EnqueuedState(payload.Id, "Re-queue prefetched job.", _innerFetcher.Queue));

                    JobFetcher.RemoveFromFetchedQueue(_innerFetcher.Redis, payload.Id, _innerFetcher.Queue);
                }
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
                    lock (_items)
                    {
                        while (_items.Count >= _count && !_cts.Token.IsCancellationRequested)
                        {
                            Monitor.Wait(_items);
                        }
                    }

                    var payload = _innerFetcher.DequeueJob(_cts.Token);

                    lock (_items)
                    {
                        _items.Add(payload);

                        _jobIsReady.Set();
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