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
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobFetcher : IJobFetcher
    {
        private readonly TimeSpan _fetchTimeout;

        private readonly IRedisClient _redis;

        public JobFetcher(
            IRedisClientsManager redisManager,
            string queue, TimeSpan? fetchTimeout = null)
        {
            _redis = redisManager.GetClient();

            Queue = queue;

            _fetchTimeout = fetchTimeout ?? TimeSpan.FromSeconds(5);
        }

        public string Queue { get; private set; }
        public IRedisClient Redis { get { return _redis; } }

        public JobPayload DequeueJob(CancellationToken cancellationToken)
        {
            string jobId;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                jobId = _redis.BlockingPopAndPushItemBetweenLists(
                    String.Format("hangfire:queue:{0}", Queue),
                    String.Format("hangfire:queue:{0}:dequeued", Queue),
                    _fetchTimeout);
            } while (jobId == null); 

            // The job was dequeued by the server. To provide reliability,
            // we should ensure, that the job will be performed and acquired
            // resources will be disposed even if the server will crash 
            // while executing one of the subsequent lines of code.

            // The job's processing is splitted into a couple of checkpoints.
            // Each checkpoint occurs after successful update of the 
            // job information in the storage. And each checkpoint describes
            // the way to perform the job when the server was crashed after
            // reaching it.

            // Checkpoint #1-1. The job was dequeued into the dequeued list,
            // that is being inspected by the DequeuedJobsWatcher instance.
            // Job's has the implicit 'Dequeued' state.

            string jobArgs = null;
            string jobType = null;
            string jobMethod = null;

            using (var pipeline = _redis.CreatePipeline())
            {
                pipeline.QueueCommand(x => x.SetEntryInHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Fetched",
                    JobHelper.ToStringTimestamp(DateTime.UtcNow)));

                pipeline.QueueCommand(
                    x => x.GetValuesFromHash(
                        String.Format("hangfire:job:{0}", jobId),
                        new[] { "Type", "Args", "Method" }),
                    x => { jobType = x[0]; jobArgs = x[1]; jobMethod = x[2]; });

                pipeline.Flush();
            }

            // Checkpoint #2. The job is in the implicit 'Fetched' state now.
            // This state stores information about fetched time. The job will
            // be re-queued when the JobTimeout will be expired.

            return new JobPayload(jobId, Queue, jobType, jobMethod, jobArgs);
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        public static void RemoveFromFetchedQueue(
            IRedisClient redis, string jobId, string queue)
        {
            using (var transaction = redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromList(
                    String.Format("hangfire:queue:{0}:dequeued", queue),
                    jobId,
                    -1));

                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Fetched"));
                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Checked"));

                transaction.Commit();
            }
        }
    }
}
