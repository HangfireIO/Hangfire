using System;
using System.Threading;
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
            Assert.Equal(TimeSpan.FromSeconds(15), options.ShutdownTimeout);
            Assert.Equal(TimeSpan.FromSeconds(15), options.SchedulePollingInterval);
            Assert.Equal(TimeSpan.FromMinutes(5), options.ServerTimeout);
            Assert.Equal(TimeSpan.FromMinutes(5), options.ServerCheckInterval);
            Assert.Equal(TimeSpan.FromSeconds(30), options.HeartbeatInterval);
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
        public void ServerTimeout_ThrowsAnException_WhenValueIsTooLarge()
        {
            var options = CreateOptions();
        
            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ServerTimeout = TimeSpan.FromHours(25));
        }

        [Fact]
        public void ServerTimeout_ThrowsAnException_WhenValueIsNegative()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ServerTimeout = TimeSpan.FromHours(-1));
        }

        [Fact]
        public void ServerTimeout_ThrowsAnException_WhenValueIsInfinite()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ServerTimeout = Timeout.InfiniteTimeSpan);
        }

        [Fact]
        public void ServerCheckInterval_ThrowsAnException_WhenValueIsTooLarge()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ServerCheckInterval = TimeSpan.FromHours(25));
        }

        [Fact]
        public void ServerCheckInterval_ThrowsAnException_WhenValueIsNegative()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ServerCheckInterval = TimeSpan.FromHours(-1));
        }

        [Fact]
        public void ServerCheckInterval_ThrowsAnException_WhenValueIsInfinite()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ServerCheckInterval = Timeout.InfiniteTimeSpan);
        }

        [Fact]
        public void HeartbeatInterval_ThrowsAnException_WhenValueIsTooLarge()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.HeartbeatInterval = TimeSpan.FromHours(25));
        }

        [Fact]
        public void HeartbeatInterval_ThrowsAnException_WhenValueIsNegative()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.HeartbeatInterval = TimeSpan.FromHours(-1));
        }

        [Fact]
        public void HeartbeatInterval_ThrowsAnException_WhenValueIsInfinite()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.HeartbeatInterval = Timeout.InfiniteTimeSpan);
        }

        [Fact]
        public void ShutdownTimeout_ThrowsAnException_WhenValueIsTooLarge()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ShutdownTimeout = TimeSpan.FromMilliseconds((double)Int32.MaxValue+1));
        }

        [Fact]
        public void ShutdownTimeout_ThrowsAnException_WhenValueIsNegative()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ShutdownTimeout = TimeSpan.FromHours(-1));
        }

        [Fact]
        public void ShutdownTimeout_DoesNotThrowAnException_WhenValueIsInfinite()
        {
            var options = CreateOptions();

            options.ShutdownTimeout = Timeout.InfiniteTimeSpan;

            Assert.Equal(Timeout.InfiniteTimeSpan, options.ShutdownTimeout);
        }

        [Fact]
        public void SchedulePollingInterval_ThrowsAnException_WhenValueIsTooLarge()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.SchedulePollingInterval = TimeSpan.FromMilliseconds((double)Int32.MaxValue + 1));
        }

        [Fact]
        public void SchedulePollingInterval_ThrowsAnException_WhenValueIsNegative()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.SchedulePollingInterval = TimeSpan.FromHours(-1));
        }

        [Fact]
        public void SchedulePollingInterval_ThrowsAnException_WhenValueIsInfinite()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.SchedulePollingInterval = Timeout.InfiniteTimeSpan);
        }

        private static BackgroundJobServerOptions CreateOptions()
        {
            return new BackgroundJobServerOptions();
        }
    }
}
