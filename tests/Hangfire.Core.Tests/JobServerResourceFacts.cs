using System;
using System.Threading.Tasks;
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
                () => resource.CapacityReporter(null, TimeSpan.FromSeconds(1)));

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
    }
}
