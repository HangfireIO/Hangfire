// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using Rsft.Lib.Msmq.MessageCounter;

namespace Hangfire.SqlServer.Msmq
{
    internal class MsmqJobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly string _pathPattern;
        private readonly IEnumerable<string> _queues;

        public MsmqJobQueueMonitoringApi(string pathPattern, IEnumerable<string> queues)
        {
            if (pathPattern == null) throw new ArgumentNullException("pathPattern");
            if (queues == null) throw new ArgumentNullException("queues");

            _pathPattern = pathPattern;
            _queues = queues;
        }

        public IEnumerable<string> GetQueues()
        {
            return _queues;
        }

        public IEnumerable<int> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            var result = new List<int>();

            using (var messageQueue = new MessageQueue(String.Format(_pathPattern, queue)))
            {
                var current = 0;
                var end = @from + perPage;
                var enumerator = messageQueue.GetMessageEnumerator2();

                var formatter = new BinaryMessageFormatter();

                while (enumerator.MoveNext())
                {
                    if (current >= @from && current < end)
                    {
                        var message = enumerator.Current;

                        message.Formatter = formatter;
                        result.Add(int.Parse((string)message.Body));
                    }

                    if (current >= end) break;

                    current++;
                }
            }

            return result;
        }

        public IEnumerable<int> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            return Enumerable.Empty<int>();
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