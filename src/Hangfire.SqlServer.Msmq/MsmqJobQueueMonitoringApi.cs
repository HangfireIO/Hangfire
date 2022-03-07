// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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