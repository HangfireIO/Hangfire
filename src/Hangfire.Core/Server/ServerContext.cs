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

namespace Hangfire.Server
{
    using System;
    using System.Collections.Generic;

    public class ServerContext
    {
        public ServerContext()
        {
            Queues = [];
            WorkerCount = -1;
            CanAllocate = true;
            AllocationState = JobServerAllocationState.Available;
            QueueAllocation = new Dictionary<string, JobServerQueueResourceSnapshot>(StringComparer.OrdinalIgnoreCase);
        }

        public int WorkerCount { get; set; }
        public string[] Queues { get; set; }
        public bool CanAllocate { get; set; }
        public string AllocationState { get; set; }
        public string AllocationReason { get; set; }
        public DateTime? AllocationCheckedAt { get; set; }
        public bool DrainMode { get; set; }
        public IDictionary<string, JobServerQueueResourceSnapshot> QueueAllocation { get; set; }
        public DateTime? AllocationStateChangedAt { get; set; }
        public DateTime? DrainStartedAt { get; set; }
        public DateTime? LastCapacityCheckFailedAt { get; set; }
        public long CapacityCheckFailureCount { get; set; }
        public string RemoteCommandState { get; set; }
    }
}
