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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Server;

namespace Hangfire
{
    public class JobServerResource : IJobServerResource, IJobServerResourceReporter, IJobServerResourceSnapshotProvider, IJobServerQueueResource
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, JobServerQueueResourceSnapshot> _queueSnapshots = new Dictionary<string, JobServerQueueResourceSnapshot>(StringComparer.OrdinalIgnoreCase);

        private Func<Task<JobServerResourceSnapshot>> _computeCapacity;
        private TimeSpan _interval;
        private JobServerResourceSnapshot _snapshot = new JobServerResourceSnapshot(true);
        private int _reporterConfigured;

        public bool CanAllocate()
        {
            return GetSnapshot().CanAllocate;
        }

        public void CapacityReporter([NotNull] Func<Task<bool>> computeCapacity, TimeSpan interval)
        {
            if (computeCapacity == null) throw new ArgumentNullException(nameof(computeCapacity));

            CapacityReporter(
                async () => new JobServerResourceSnapshot(
                    await computeCapacity().ConfigureAwait(false),
                    null,
                    DateTime.UtcNow),
                interval);
        }

        public void CapacityReporter([NotNull] Func<Task<JobServerResourceSnapshot>> computeCapacity, TimeSpan interval)
        {
            if (computeCapacity == null) throw new ArgumentNullException(nameof(computeCapacity));
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval), "The interval must be positive.");

            _computeCapacity = computeCapacity;
            _interval = interval;
            SetSnapshot(new JobServerResourceSnapshot(false, null, null));
            Volatile.Write(ref _reporterConfigured, 1);
        }

        public void Drain([CanBeNull] string reason)
        {
            SetSnapshot(new JobServerResourceSnapshot(
                false,
                String.IsNullOrWhiteSpace(reason) ? "Manually drained" : reason,
                DateTime.UtcNow,
                JobServerAllocationState.Draining,
                drainMode: true));
        }

        public void Resume()
        {
            SetSnapshot(new JobServerResourceSnapshot(true, null, DateTime.UtcNow));
        }

        public void SetQueueState([NotNull] string queue, bool canAllocate, [CanBeNull] string reason = null)
        {
            if (String.IsNullOrWhiteSpace(queue)) throw new ArgumentNullException(nameof(queue));

            lock (_syncRoot)
            {
                _queueSnapshots[queue] = new JobServerQueueResourceSnapshot(queue, canAllocate, reason);
            }
        }

        public void ClearQueueState([NotNull] string queue)
        {
            if (String.IsNullOrWhiteSpace(queue)) throw new ArgumentNullException(nameof(queue));

            lock (_syncRoot)
            {
                _queueSnapshots.Remove(queue);
            }
        }

        public JobServerResourceSnapshot GetSnapshot()
        {
            lock (_syncRoot)
            {
                return _snapshot;
            }
        }

        public string[] GetAvailableQueues([NotNull] string[] queues)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));

            lock (_syncRoot)
            {
                return queues.Where(queue =>
                    !_queueSnapshots.TryGetValue(queue, out var snapshot) || snapshot.CanAllocate).ToArray();
            }
        }

        public IDictionary<string, JobServerQueueResourceSnapshot> GetQueueSnapshots([NotNull] string[] queues)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));

            lock (_syncRoot)
            {
                return queues
                    .Select(queue => _queueSnapshots.TryGetValue(queue, out var snapshot)
                        ? snapshot
                        : new JobServerQueueResourceSnapshot(queue, true))
                    .ToDictionary(static snapshot => snapshot.Queue, StringComparer.OrdinalIgnoreCase);
            }
        }

        public static JobServerResource FromMemoryLimit(long maxManagedMemoryBytes, TimeSpan interval)
        {
            if (maxManagedMemoryBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxManagedMemoryBytes));

            var resource = new JobServerResource();
            resource.CapacityReporter(() =>
            {
                var memoryBytes = GC.GetTotalMemory(forceFullCollection: false);
                var canAllocate = memoryBytes <= maxManagedMemoryBytes;
                return Task.FromResult(new JobServerResourceSnapshot(
                    canAllocate,
                    canAllocate ? null : "Memory pressure",
                    DateTime.UtcNow));
            }, interval);

            return resource;
        }

        public static JobServerResource FromDiskFreeSpace([NotNull] string path, long minFreeBytes, TimeSpan interval)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (minFreeBytes < 0) throw new ArgumentOutOfRangeException(nameof(minFreeBytes));

            var resource = new JobServerResource();
            resource.CapacityReporter(() =>
            {
#if NETSTANDARD1_3
                return Task.FromResult(new JobServerResourceSnapshot(
                    true,
                    "Disk free-space metric unsupported",
                    DateTime.UtcNow));
#else
                var root = Path.GetPathRoot(Path.GetFullPath(path));
                var drive = new DriveInfo(root);
                var canAllocate = drive.AvailableFreeSpace >= minFreeBytes;
                return Task.FromResult(new JobServerResourceSnapshot(
                    canAllocate,
                    canAllocate ? null : "Disk pressure",
                    DateTime.UtcNow));
#endif
            }, interval);

            return resource;
        }

        public static JobServerResource FromCpuLoad(double maxCpuLoad, TimeSpan interval, bool failClosedWhenUnsupported = false)
        {
            if (maxCpuLoad < 0 || maxCpuLoad > 1) throw new ArgumentOutOfRangeException(nameof(maxCpuLoad));

            var resource = new JobServerResource();
            resource.CapacityReporter(() => Task.FromResult(new JobServerResourceSnapshot(
                !failClosedWhenUnsupported,
                "CPU load metric unsupported",
                DateTime.UtcNow)), interval);

            return resource;
        }

        public static JobServerResource FromComposite(TimeSpan interval, [NotNull] params IJobServerResource[] resources)
        {
            if (resources == null) throw new ArgumentNullException(nameof(resources));

            var resource = new JobServerResource();
            resource.CapacityReporter(() =>
            {
                foreach (var child in resources)
                {
                    if (child == null) continue;

                    if (!child.CanAllocate())
                    {
                        var childSnapshot = (child as IJobServerResourceSnapshotProvider)?.GetSnapshot();
                        return Task.FromResult(new JobServerResourceSnapshot(
                            false,
                            childSnapshot?.Reason ?? "Resource constrained",
                            DateTime.UtcNow));
                    }
                }

                return Task.FromResult(new JobServerResourceSnapshot(true, null, DateTime.UtcNow));
            }, interval);

            return resource;
        }

        TimeSpan IJobServerResourceReporter.Interval => _interval;

        bool IJobServerResourceReporter.IsConfigured => Volatile.Read(ref _reporterConfigured) == 1;

        async Task<JobServerResourceSnapshot> IJobServerResourceReporter.ComputeCapacityAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var computeCapacity = _computeCapacity;
            if (computeCapacity == null) return GetSnapshot();

            var result = await computeCapacity().ConfigureAwait(false) ?? new JobServerResourceSnapshot(false, "Capacity check failed", DateTime.UtcNow);
            SetSnapshot(result);

            return result;
        }

        void IJobServerResourceReporter.ReportCapacityCheckFailure(Exception exception)
        {
            var snapshot = GetSnapshot();
            if (!snapshot.CanAllocate && !snapshot.CheckedAt.HasValue)
            {
                SetSnapshot(new JobServerResourceSnapshot(
                    false,
                    "Capacity check failed",
                    DateTime.UtcNow,
                    JobServerAllocationState.ResourceConstrained,
                    drainMode: false));
            }
        }

        private void SetSnapshot(JobServerResourceSnapshot snapshot)
        {
            lock (_syncRoot)
            {
                _snapshot = snapshot;
            }
        }
    }
}
