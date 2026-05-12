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
using Hangfire.Annotations;

namespace Hangfire
{
    public sealed class JobServerResourceSnapshot
    {
        public JobServerResourceSnapshot()
        {
            CanAllocate = true;
            AllocationState = JobServerAllocationState.Available;
        }

        public JobServerResourceSnapshot(bool canAllocate)
            : this(canAllocate, null, null, canAllocate ? JobServerAllocationState.Available : JobServerAllocationState.ResourceConstrained, false)
        {
        }

        public JobServerResourceSnapshot(bool canAllocate, [CanBeNull] string reason, DateTime? checkedAt)
            : this(canAllocate, reason, checkedAt, canAllocate ? JobServerAllocationState.Available : JobServerAllocationState.ResourceConstrained, false)
        {
        }

        public JobServerResourceSnapshot(
            bool canAllocate,
            [CanBeNull] string reason,
            DateTime? checkedAt,
            [CanBeNull] string allocationState,
            bool drainMode)
        {
            CanAllocate = canAllocate;
            Reason = reason;
            CheckedAt = checkedAt;
            AllocationState = allocationState ?? (canAllocate ? JobServerAllocationState.Available : JobServerAllocationState.ResourceConstrained);
            DrainMode = drainMode;
        }

        public bool CanAllocate { get; set; }
        public string Reason { get; set; }
        public DateTime? CheckedAt { get; set; }
        public string AllocationState { get; set; }
        public bool DrainMode { get; set; }
        public DateTime? StateChangedAt { get; set; }
        public DateTime? DrainStartedAt { get; set; }
        public DateTime? LastCapacityCheckFailedAt { get; set; }
        public long CapacityCheckFailureCount { get; set; }
    }
}
