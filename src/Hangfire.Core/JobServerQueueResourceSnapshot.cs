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

using Hangfire.Annotations;

namespace Hangfire
{
    using System;

    public sealed class JobServerQueueResourceSnapshot
    {
        public JobServerQueueResourceSnapshot()
        {
        }

        public JobServerQueueResourceSnapshot([NotNull] string queue, bool canAllocate)
            : this(queue, canAllocate, null)
        {
        }

        public JobServerQueueResourceSnapshot([NotNull] string queue, bool canAllocate, [CanBeNull] string reason)
        {
            Queue = queue;
            CanAllocate = canAllocate;
            Reason = reason;
        }

        public string Queue { get; set; }
        public bool CanAllocate { get; set; }
        public string Reason { get; set; }
        public bool DrainMode { get; set; }
        public DateTime? StateChangedAt { get; set; }
    }
}
