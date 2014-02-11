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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Server.Fetching
{
    internal class PrioritizedJobFetcher : IJobFetcher
    {
        private readonly DisposableCollection<PrefetchJobFetcher> _fetchers
            = new DisposableCollection<PrefetchJobFetcher>();

        public PrioritizedJobFetcher(
            IRedisClientsManager redisClientsManager,
            IEnumerable<string> queues, 
            int prefetchCount,
            TimeSpan fetchTimeout)
        {
            foreach (var queue in queues)
            {
                _fetchers.Add(new PrefetchJobFetcher(
                    new JobFetcher(redisClientsManager.GetClient(), queue, fetchTimeout), prefetchCount));
            }
        }

        public void Dispose()
        {
            _fetchers.Dispose();
        }

        public QueuedJob DequeueJob(CancellationToken cancellationToken)
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