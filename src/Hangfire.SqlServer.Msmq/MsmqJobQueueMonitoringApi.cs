// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using MQTools;

namespace Hangfire.SqlServer.Msmq
{
    internal class MsmqJobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly string _pathPattern;
        private readonly IEnumerable<string> _queues;

        public MsmqJobQueueMonitoringApi(string pathPattern, IEnumerable<string> queues)
        {
            if (pathPattern == null) throw new ArgumentNullException(nameof(pathPattern));
            if (queues == null) throw new ArgumentNullException(nameof(queues));

            _pathPattern = pathPattern;
            _queues = queues;
        }

        public IEnumerable<string> GetQueues()
        {
            return _queues;
        }

        public IEnumerable<long> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            var result = new List<long>();

            using (var messageQueue = new MessageQueue(String.Format(_pathPattern, queue)))
            {
                var current = 0;
                var end = from + perPage;
                var enumerator = messageQueue.GetMessageEnumerator2();

                while (enumerator.MoveNext())
                {
                    if (current >= from && current < end)
                    {
                        var message = enumerator.Current;
                        if (message == null) continue;

                        result.Add(long.Parse(message.Label));
                    }

                    if (current >= end) break;

                    current++;
                }
            }

            return result;
        }

        public IEnumerable<long> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            return Enumerable.Empty<long>();
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            using (var messageQueue = new MessageQueue(String.Format(_pathPattern, queue)))
            {                
                return new EnqueuedAndFetchedCountDto
                {
                    EnqueuedCount = (int?)messageQueue.GetCount()
                };
            }
        }
    }
}