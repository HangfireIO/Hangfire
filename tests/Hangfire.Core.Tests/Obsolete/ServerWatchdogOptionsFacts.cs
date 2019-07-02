using System;
using System.Threading;
using Hangfire.Server;
using Xunit;

namespace Hangfire.Core.Tests.Obsolete
{
    public class ServerWatchdogOptionsFacts
    {
        [Fact]
        public void Ctor_InitializeProperties_WithCorrectValues()
        {
            var options = CreateOptions();

            Assert.Equal(TimeSpan.FromMinutes(5), options.CheckInterval);
            Assert.Equal(TimeSpan.FromMinutes(5), options.ServerTimeout);
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
                () => options.ServerTimeout = TimeSpan.FromHours(-5));
        }

        [Fact]
        public void ServerTimeout_ThrowsAnException_WhenValueIsInfinite()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ServerTimeout = Timeout.InfiniteTimeSpan);
        }

        [Fact]
        public void CheckInterval_ThrowsAnException_WhenValueIsNegative()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.CheckInterval = TimeSpan.FromHours(-5));
        }

        [Fact]
        public void CheckInterval_ThrowsAnException_WhenValueIsTooLarge()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.CheckInterval = TimeSpan.FromHours(25));
        }

        [Fact]
        public void CheckInterval_ThrowsAnException_WhenValueIsInfinite()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.CheckInterval = Timeout.InfiniteTimeSpan);
        }

        [Fact]
        public void CheckInterval_DoesNotThrowException_WhenValueIsZero()
        {
            var options = CreateOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.CheckInterval = Timeout.InfiniteTimeSpan);
        }

        private static ServerWatchdogOptions CreateOptions()
        {
            return new ServerWatchdogOptions();
        }
    }
}