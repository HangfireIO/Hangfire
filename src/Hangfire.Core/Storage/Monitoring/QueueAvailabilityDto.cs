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

using System.Collections.Generic;

namespace Hangfire.Storage.Monitoring
{
    public sealed class QueueAvailabilityDto
    {
        public string Queue { get; set; }

        public string TenantId { get; set; }

        public int AvailableServers { get; set; }

        public int ConstrainedServers { get; set; }

        public int DrainingServers { get; set; }

        public int OfflineServers { get; set; }

        public IDictionary<string, int> ConstrainedByReason { get; set; }
    }
}
