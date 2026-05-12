using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Storage
{
    public class ResourceMonitoringFacts
    {
        [Fact]
        public void GetQueueAvailability_GroupsServersByQueueAndAllocationState()
        {
            var connection = new Mock<JobStorageConnection> { CallBase = true };
            var now = new DateTime(2026, 05, 12, 10, 00, 00, DateTimeKind.Utc);

            var result = connection.Object.GetQueueAvailability(new[]
            {
                new ServerDto
                {
                    Name = "available",
                    Queues = new[] { "default" },
                    Heartbeat = now,
                    CanAllocate = true
                },
                new ServerDto
                {
                    Name = "constrained",
                    Queues = new[] { "default" },
                    Heartbeat = now,
                    CanAllocate = false,
                    AllocationReason = "CPU pressure"
                },
                new ServerDto
                {
                    Name = "draining",
                    Queues = new[] { "default" },
                    Heartbeat = now,
                    CanAllocate = true,
                    DrainMode = true,
                    AllocationReason = "deployment"
                },
                new ServerDto
                {
                    Name = "offline",
                    Queues = new[] { "default" },
                    Heartbeat = now.AddMinutes(-10),
                    CanAllocate = true
                }
            }, now, TimeSpan.FromMinutes(5)).Single();

            Assert.Equal("default", result.Queue);
            Assert.Equal(1, result.AvailableServers);
            Assert.Equal(2, result.ConstrainedServers);
            Assert.Equal(1, result.DrainingServers);
            Assert.Equal(1, result.OfflineServers);
            Assert.Equal(1, result.ConstrainedByReason["CPU pressure"]);
            Assert.Equal(1, result.ConstrainedByReason["deployment"]);
        }

        [Fact]
        public void GetQueueAvailability_UsesQueueLevelDrainReason()
        {
            var connection = new Mock<JobStorageConnection> { CallBase = true };
            var now = new DateTime(2026, 05, 12, 10, 00, 00, DateTimeKind.Utc);

            var result = connection.Object.GetQueueAvailability(new[]
            {
                new ServerDto
                {
                    Name = "server",
                    Queues = new[] { "default", "images" },
                    Heartbeat = now,
                    CanAllocate = true,
                    QueueAllocation = new Dictionary<string, JobServerQueueResourceSnapshot>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["images"] = new JobServerQueueResourceSnapshot("images", false, "GPU maintenance")
                        {
                            DrainMode = true
                        }
                    }
                }
            }, now, TimeSpan.FromMinutes(5)).ToArray();

            var defaultQueue = result.Single(x => x.Queue == "default");
            var imagesQueue = result.Single(x => x.Queue == "images");

            Assert.Equal(1, defaultQueue.AvailableServers);
            Assert.Equal(0, defaultQueue.ConstrainedServers);
            Assert.Equal(0, defaultQueue.DrainingServers);

            Assert.Equal(0, imagesQueue.AvailableServers);
            Assert.Equal(1, imagesQueue.ConstrainedServers);
            Assert.Equal(1, imagesQueue.DrainingServers);
            Assert.Equal(1, imagesQueue.ConstrainedByReason["GPU maintenance"]);
        }

        [Fact]
        public void GetQueueAvailability_SeparatesTenantQueues()
        {
            var connection = new Mock<JobStorageConnection> { CallBase = true };
            var now = new DateTime(2026, 05, 12, 10, 00, 00, DateTimeKind.Utc);

            var result = connection.Object.GetQueueAvailability(new[]
            {
                new ServerDto { TenantId = "tenant-a", Queues = new[] { "default" }, Heartbeat = now, CanAllocate = true },
                new ServerDto { TenantId = "tenant-b", Queues = new[] { "default" }, Heartbeat = now, CanAllocate = false }
            }, now, TimeSpan.FromMinutes(5)).ToArray();

            Assert.Equal(2, result.Length);
            Assert.Equal("tenant-a", result[0].TenantId);
            Assert.Equal(1, result[0].AvailableServers);
            Assert.Equal("tenant-b", result[1].TenantId);
            Assert.Equal(1, result[1].ConstrainedServers);
        }
    }
}
