using System;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class BackgroundJobServerOptionsFacts
    {
        [Fact]
        public void Ctor_InitializeProperties_WithCorrectValues()
        {
            var options = CreateOptions();

            Assert.Equal(Math.Min(Environment.ProcessorCount * 5, 20), options.WorkerCount);
            Assert.Equal(EnqueuedState.DefaultQueue, options.Queues[0]);
            Assert.True(options.ShutdownTimeout > TimeSpan.Zero);
            Assert.True(options.SchedulePollingInterval > TimeSpan.Zero);
        }

        [Fact]
        public void WorkerCount_ThrowsAnException_WhenValueIsEqualToZero()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.WorkerCount = 0);
        }

        [Fact]
        public void WorkerCount_ThrowsAnException_WhenValueIsNegative()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.WorkerCount = -1);
        }

        [Fact]
        public void Queues_ThrowsAnException_WhenValueIsNull()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentNullException>(
                () => options.Queues = null);
        }

        [Fact]
        public void Queues_ThrowsAnException_WhenGivenArrayIsEmpty()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentException>(
                () => options.Queues = new string[0]);
        }

        private static BackgroundJobServerOptions CreateOptions()
        {
            return new BackgroundJobServerOptions();
        }
    }
}
