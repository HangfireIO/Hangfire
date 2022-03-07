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

using System.Collections.Generic;

namespace Hangfire.SqlServer.Msmq
{
    internal class MsmqJobQueueProvider : IPersistentJobQueueProvider
    {
        private readonly MsmqJobQueue _jobQueue;
        private readonly MsmqJobQueueMonitoringApi _monitoringApi;

        public MsmqJobQueueProvider(string pathPattern, IEnumerable<string> queues, MsmqTransactionType transactionType)
        {
            _jobQueue = new MsmqJobQueue(pathPattern, transactionType);
            _monitoringApi = new MsmqJobQueueMonitoringApi(pathPattern, queues);
        }

        public IPersistentJobQueue GetJobQueue()
        {
            return _jobQueue;
        }

        public IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi()
        {
            return _monitoringApi;
        }
    }
}