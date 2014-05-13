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
using System.Messaging;

namespace HangFire.SqlServer
{
    internal class MsmqJobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly string _pathPattern;

        public MsmqJobQueueMonitoringApi(string pathPattern)
        {
            _pathPattern = pathPattern;
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
                var count = 0;
                var enumerator = messageQueue.GetMessageEnumerator2();

                while (enumerator.MoveNext())
                {
                    count++; 
                }

                return new EnqueuedAndFetchedCountDto
                {
                    EnqueuedCount = count
                };
            }
        }
    }
}