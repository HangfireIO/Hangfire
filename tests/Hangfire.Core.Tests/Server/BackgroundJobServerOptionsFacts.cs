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

            Assert.Equal(Math.Min(Environment.ProcessorCount * 5, 40), options.WorkerCount);
            Assert.Equal(Environment.MachineName, options.ServerName);
            Assert.Equal(EnqueuedState.DefaultQueue, options.Queues[0]);
            Assert.True(options.ShutdownTimeout > TimeSpan.Zero);
            Assert.True(options.SchedulePollingInterval > TimeSpan.Zero);
        }

        [Fact]
        public void ServerName_ThrowsAnException_WhenValueIsNull()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentNullException>(
                () => options.ServerName = null);
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

        [Fact]
        public void ServerWatchDogOptions_IsNonNullByDefault()
        {
            var options = CreateOptions();

            Assert.NotNull(options.ServerWatchdogOptions);
        }

        [Fact]
        public void ServerName_EqualsToMachineName_ByDefault()
        {
            var options = CreateOptions();

            Assert.NotEmpty(options.ServerName);
            Assert.Equal(Environment.MachineName, options.ServerName);
        }

        private static BackgroundJobServerOptions CreateOptions()
        {
            return new BackgroundJobServerOptions();
        }
    }
}
