using System;
using System.Threading;
using Hangfire.Server;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class BackgroundProcessingServerOptionsFacts
    {
        [Fact]
        public void ShutdownTimeout_ThrowsAnException_WhenValueIsTooLarge()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ShutdownTimeout = TimeSpan.FromMilliseconds((double)Int32.MaxValue + 1));
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

        private static BackgroundProcessingServerOptions CreateOptions()
        {
            return new BackgroundProcessingServerOptions();
        }
    }
}