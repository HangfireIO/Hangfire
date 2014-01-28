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
using System.Threading;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Server.Fetching
{
    internal class JobFetcher : IJobFetcher
    {
        private readonly JobQueue _queue;
        private readonly IRedisClient _redis;

        public JobFetcher(
            IRedisClientsManager redisManager,
            string queue, TimeSpan? fetchTimeout = null)
        {
            _redis = redisManager.GetClient();
            Queue = queue;

            _queue = new JobQueue(_redis, queue, fetchTimeout ?? TimeSpan.FromSeconds(5));
        }

        public string Queue { get; private set; }
        public IRedisClient Redis { get { return _redis; } }

        public QueuedJob DequeueJob(CancellationToken cancellationToken)
        {
            return _queue.TakeNext(cancellationToken);
        }

        public void Dispose()
        {
            _redis.Dispose();
        }
    }
}
