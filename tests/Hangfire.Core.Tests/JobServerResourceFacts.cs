using System;
using System.Threading.Tasks;
using Hangfire.Server;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class JobServerResourceFacts
    {
        [Fact]
        public void CanAllocate_ReturnsTrue_ByDefault()
        {
            var resource = new JobServerResource();

            Assert.True(resource.CanAllocate());
        }

        [Fact]
        public void CapacityReporter_Throws_WhenComputeCapacityIsNull()
        {
            var resource = new JobServerResource();

            var exception = Assert.Throws<ArgumentNullException>(
                () => resource.CapacityReporter((Func<Task<bool>>)null, TimeSpan.FromSeconds(1)));

            Assert.Equal("computeCapacity", exception.ParamName);
        }

        [Fact]
        public void CapacityReporter_Throws_WhenIntervalIsNotPositive()
        {
            var resource = new JobServerResource();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => resource.CapacityReporter(() => Task.FromResult(true), TimeSpan.Zero));

            Assert.Equal("interval", exception.ParamName);
        }

        [Fact]
        public void CapacityReporter_MakesResourceUnavailable_UntilFirstSuccessfulComputation()
        {
            var resource = new JobServerResource();

            resource.CapacityReporter(() => Task.FromResult(true), TimeSpan.FromSeconds(1));

            Assert.False(resource.CanAllocate());
        }

        [Fact]
        public void Drain_MakesResourceUnavailable_WithDrainState()
        {
            var resource = new JobServerResource();

            resource.Drain("deployment");

            var snapshot = resource.GetSnapshot();
            Assert.False(snapshot.CanAllocate);
            Assert.True(snapshot.DrainMode);
            Assert.Equal(JobServerAllocationState.Draining, snapshot.AllocationState);
            Assert.Equal("deployment", snapshot.Reason);
            Assert.NotNull(snapshot.StateChangedAt);
            Assert.NotNull(snapshot.DrainStartedAt);
        }

        [Fact]
        public void Resume_MakesResourceAvailable()
        {
            var resource = new JobServerResource();
            resource.Drain("deployment");

            resource.Resume();

            Assert.True(resource.CanAllocate());
            Assert.Equal(JobServerAllocationState.Available, resource.GetSnapshot().AllocationState);
        }

        [Fact]
        public void GetAvailableQueues_FiltersPausedQueues()
        {
            var resource = new JobServerResource();
            resource.SetQueueState("critical", false, "CPU pressure");

            var queues = resource.GetAvailableQueues(new[] { "critical", "default" });

            Assert.Equal(new[] { "default" }, queues);
        }

        [Fact]
        public void DrainQueue_FiltersOnlyDrainedQueue()
        {
            var resource = new JobServerResource();

            resource.DrainQueue("critical", "maintenance");

            var queues = resource.GetAvailableQueues(new[] { "critical", "default" });
            var snapshots = resource.GetQueueSnapshots(new[] { "critical", "default" });

            Assert.Equal(new[] { "default" }, queues);
            Assert.False(snapshots["critical"].CanAllocate);
            Assert.True(snapshots["critical"].DrainMode);
            Assert.Equal("maintenance", snapshots["critical"].Reason);
            Assert.True(snapshots["default"].CanAllocate);
        }

        [Fact]
        public void ResumeQueue_MakesQueueAvailableAgain()
        {
            var resource = new JobServerResource();
            resource.DrainQueue("critical", "maintenance");

            resource.ResumeQueue("critical");

            var queues = resource.GetAvailableQueues(new[] { "critical", "default" });
            Assert.Equal(new[] { "critical", "default" }, queues);
        }

        [Fact]
        public async Task CpuProvider_FailsOpen_WhenUnsupportedByDefault()
        {
            var resource = JobServerResource.FromCpuLoad(0.80, TimeSpan.FromSeconds(1));
            var reporter = (IJobServerResourceReporter)resource;

            var snapshot = await reporter.ComputeCapacityAsync(default);

            Assert.True(snapshot.CanAllocate);
            Assert.Equal("CPU load metric unsupported", snapshot.Reason);
        }

        [Fact]
        public async Task CpuProvider_FailsClosed_WhenUnsupportedAndConfigured()
        {
            var resource = JobServerResource.FromCpuLoad(0.80, TimeSpan.FromSeconds(1), failClosedWhenUnsupported: true);
            var reporter = (IJobServerResourceReporter)resource;

            var snapshot = await reporter.ComputeCapacityAsync(default);

            Assert.False(snapshot.CanAllocate);
            Assert.Equal("CPU load metric unsupported", snapshot.Reason);
        }

        [Fact]
        public async Task DiskProvider_ReportsAvailable_WhenFreeSpaceIsAboveThreshold()
        {
            var resource = JobServerResource.FromDiskFreeSpace(AppContext.BaseDirectory, 0, TimeSpan.FromSeconds(1));
            var reporter = (IJobServerResourceReporter)resource;

            var snapshot = await reporter.ComputeCapacityAsync(default);

            Assert.True(snapshot.CanAllocate);
            Assert.Null(snapshot.Reason);
        }

        [Fact]
        public async Task CompositeProvider_ReportsFirstConstrainedChildReason()
        {
            var constrained = new JobServerResource();
            constrained.Drain("Memory pressure");
            var available = new JobServerResource();
            var resource = JobServerResource.FromComposite(TimeSpan.FromSeconds(1), available, constrained);
            var reporter = (IJobServerResourceReporter)resource;

            var snapshot = await reporter.ComputeCapacityAsync(default);

            Assert.False(snapshot.CanAllocate);
            Assert.Equal("Memory pressure", snapshot.Reason);
        }
    }
}
