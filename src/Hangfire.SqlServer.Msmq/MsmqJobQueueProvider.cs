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

using System.Collections.Generic;
using System.Data;

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