// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Threading;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;
using HangFire.Storage.Redis;
using ServiceStack.Logging;

namespace HangFire.Server.Fetching
{
    internal class PrefetchJobFetcher : IJobFetcher, IStoppable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PrefetchJobFetcher));

        private readonly JobFetcher _innerFetcher;
        private readonly int _count;

        private readonly BlockingCollection<QueuedJob> _items
            = new BlockingCollection<QueuedJob>(new ConcurrentQueue<QueuedJob>());

        private readonly Thread _prefetchThread;
        private readonly ManualResetEventSlim _jobIsReady
            = new ManualResetEventSlim(false);
        private readonly CancellationTokenSource _cts
            = new CancellationTokenSource();

        private bool _stopSent;

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

        public QueuedJob DequeueJob(CancellationToken cancellationToken)
        {
            var job = _items.Take(cancellationToken);

            lock (_items)
            {
                if (_items.Count == 0)
                {
                    _jobIsReady.Reset();
                }

                Monitor.Pulse(_items);
            }

            return job;
        }

        public void Stop()
        {
            _stopSent = true;

            _cts.Cancel();

            lock (_items)
            {
                Monitor.Pulse(_items);
            }
        }

        public void Dispose()
        {
            if (!_stopSent)
            {
                Stop();
            }

            _prefetchThread.Join();

            RequeuePrefetched();

            _innerFetcher.Dispose();

            _jobIsReady.Dispose();
            _cts.Dispose();
        }

        private void RequeuePrefetched()
        {
            if (_items.Count == 0)
            {
                return;
            }

            try
            {
                var enqueuedState = new EnqueuedState("Re-queue prefetched job");
                var stateMachine = new StateMachine(new RedisStorageConnection(_innerFetcher.Redis));

                foreach (var job in _items)
                {
                    stateMachine.ChangeState(job.Payload.Id, enqueuedState);
                    job.Complete(_innerFetcher.Redis, canceled: true);
                }

                Logger.InfoFormat("{0} prefetched jobs were re-queued.", _items.Count);
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occured while trying to re-queue prefetched jobs. Some prefetched jobs may remain in the dequeue list.", ex);
            }
        }

        private void Prefetch()
        {
            try
            {
                Logger.InfoFormat("Job fetcher for the '{0}' queue has been started.", _innerFetcher.Queue);

                while (true)
                {
                    lock (_items)
                    {
                        while (_items.Count >= _count && !_cts.Token.IsCancellationRequested)
                        {
                            Monitor.Wait(_items);
                        }
                    }

                    JobServer.RetryOnException(
                        () =>
                        {
                            var payload = _innerFetcher.DequeueJob(_cts.Token);

                            lock (_items)
                            {
                                _items.Add(payload);

                                _jobIsReady.Set();
                            }
                        }, _cts.Token.WaitHandle);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Job fetcher was stopped.");
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected exception caught. Jobs will not be fetched.", ex);
            }
        }
    }
}