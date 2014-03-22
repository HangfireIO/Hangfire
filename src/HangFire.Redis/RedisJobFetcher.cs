// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HangFire.Common;
using HangFire.Server;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis
{
    internal class RedisJobFetcher : IJobFetcher
    {
        private readonly IRedisClient _redis;
        private readonly IList<string> _queueNames;
        private readonly TimeSpan _fetchTimeout;

        public RedisJobFetcher(
            IRedisClient redis, 
            IEnumerable<string> queueNames, 
            TimeSpan fetchTimeout)
        {
            _redis = redis;
            _queueNames = queueNames.ToList();
            _fetchTimeout = fetchTimeout;
        }

        public JobPayload FetchNextJob(CancellationToken cancellationToken)
        {
            string jobId;
            string queueName;
            var queueIndex = 0;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                queueIndex = (queueIndex + 1) % _queueNames.Count;
                queueName = _queueNames[queueIndex];

                var queueKey = RedisStorage.Prefix + String.Format("queue:{0}", queueName);
                var fetchedKey = RedisStorage.Prefix + String.Format("queue:{0}:dequeued", queueName);

                if (queueIndex == 0)
                {
                    jobId = _redis.BlockingPopAndPushItemBetweenLists(
                        queueKey,
                        fetchedKey,
                        _fetchTimeout);
                }
                else
                {
                    jobId = _redis.PopAndPushItemBetweenLists(
                        queueKey, fetchedKey);
                }
                
            } while (jobId == null);

            // The job was fetched by the server. To provide reliability,
            // we should ensure, that the job will be performed and acquired
            // resources will be disposed even if the server will crash 
            // while executing one of the subsequent lines of code.

            // The job's processing is splitted into a couple of checkpoints.
            // Each checkpoint occurs after successful update of the 
            // job information in the storage. And each checkpoint describes
            // the way to perform the job when the server was crashed after
            // reaching it.

            // Checkpoint #1-1. The job was fetched into the fetched list,
            // that is being inspected by the FetchedJobsWatcher instance.
            // Job's has the implicit 'Fetched' state.

            string type = null;
            string method = null;
            string parameterTypes = null;
            string arguments = null;
            string args = null;

            using (var pipeline = _redis.CreatePipeline())
            {
                pipeline.QueueCommand(x => x.SetEntryInHash(
                    String.Format(RedisStorage.Prefix + "job:{0}", jobId),
                    "Fetched",
                    JobHelper.ToStringTimestamp(DateTime.UtcNow)));

                // ServiceStack.Redis library could not queue a command,
                // that returns IDictionary, so, let's build it using MGET.
                pipeline.QueueCommand(
                    x => x.GetValuesFromHash(
                        RedisStorage.Prefix + String.Format("job:{0}", jobId),
                        new[] { "Type", "Args", "Method", "Arguments", "ParameterTypes" }),
                    x =>
                    {
                        type = x[0];
                        method = x[2];
                        parameterTypes = x[4];
                        args = x[1];
                        arguments = x[3];
                    });

                pipeline.Flush();
            }

            // Checkpoint #2. The job is in the implicit 'Fetched' state now.
            // This state stores information about fetched time. The job will
            // be re-queued when the JobTimeout will be expired.

            var invocationData = new InvocationData(type, method, parameterTypes);

            return new JobPayload(jobId, queueName, invocationData)
            {
                Args = args,
                Arguments = arguments
            };
        }
    }
}