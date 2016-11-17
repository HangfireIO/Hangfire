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
            var options = new ServerWatchdogOptions();

            Assert.True(options.CheckInterval >= TimeSpan.Zero || options.CheckInterval == Timeout.InfiniteTimeSpan);
            Assert.True(options.ServerTimeout > TimeSpan.Zero && options.ServerTimeout <= TimeSpan.FromHours(24));
        }

        [Fact]
        public void ServerTimeout_ThrowsAnException_WhenValueIsTooLarge()
        {
            var options = new ServerWatchdogOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ServerTimeout = TimeSpan.FromHours(25));
        }
    }
}