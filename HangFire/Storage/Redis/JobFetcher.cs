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
using System.Threading;
using HangFire.Common;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Server.Fetching
{
    internal class JobFetcher : IJobFetcher
    {
        private readonly IRedisClient _redis;
        private readonly TimeSpan? _fetchTimeout;

        public JobFetcher(
            IRedisClient redis,
            string queue, TimeSpan? fetchTimeout = null)
        {
            _redis = redis;
            _fetchTimeout = fetchTimeout;
            Queue = queue;
        }

        public string Queue { get; private set; }
        public IRedisClient Redis { get { return _redis; } }

        public QueuedJob DequeueJob(CancellationToken cancellationToken)
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

            Dictionary<string, string> job = null;

            using (var pipeline = _redis.CreatePipeline())
            {
                pipeline.QueueCommand(x => x.SetEntryInHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Fetched",
                    JobHelper.ToStringTimestamp(DateTime.UtcNow)));

                // ServiceStack.Redis library could not queue a command,
                // that returns IDictionary, so, let's build it using MGET.
                pipeline.QueueCommand(
                    x => x.GetValuesFromHash(
                        String.Format("hangfire:job:{0}", jobId),
                        new[] { "Type", "Args", "Method", "Arguments", "ParameterTypes" }),
                    x => job = new Dictionary<string, string>
                    {
                        { "Type", x[0] },
                        { "Args", x[1] },
                        { "Method", x[2] },
                        { "Arguments", x[3] },
                        { "ParameterTypes", x[4] }
                    });

                pipeline.Flush();
            }

            // Checkpoint #2. The job is in the implicit 'Fetched' state now.
            // This state stores information about fetched time. The job will
            // be re-queued when the JobTimeout will be expired.

            return new QueuedJob(new JobPayload(jobId, Queue, job));
        }

        public void Dispose()
        {
            _redis.Dispose();
        }
    }
}
