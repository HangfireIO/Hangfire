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
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Server;

namespace Hangfire
{
    public class JobServerResource : IJobServerResource, IJobServerResourceReporter
    {
        private Func<Task<bool>> _computeCapacity;
        private TimeSpan _interval;
        private int _canAllocate = 1;
        private int _reporterConfigured;

        public bool CanAllocate()
        {
            return Volatile.Read(ref _canAllocate) == 1;
        }

        public void CapacityReporter([NotNull] Func<Task<bool>> computeCapacity, TimeSpan interval)
        {
            if (computeCapacity == null) throw new ArgumentNullException(nameof(computeCapacity));
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval), "The interval must be positive.");

            _computeCapacity = computeCapacity;
            _interval = interval;
            Volatile.Write(ref _canAllocate, 0);
            Volatile.Write(ref _reporterConfigured, 1);
        }

        TimeSpan IJobServerResourceReporter.Interval => _interval;

        bool IJobServerResourceReporter.IsConfigured => Volatile.Read(ref _reporterConfigured) == 1;

        async Task<bool> IJobServerResourceReporter.ComputeCapacityAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var computeCapacity = Volatile.Read(ref _computeCapacity);
            if (computeCapacity == null) return CanAllocate();

            var result = await computeCapacity().ConfigureAwait(false);
            Volatile.Write(ref _canAllocate, result ? 1 : 0);

            return result;
        }
    }
}
