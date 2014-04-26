using System;
using HangFire.Server;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class ServerComponentRunnerOptionsFacts
    {
        [Fact]
        public void MaxRetryAttempts_Set_ThrowsAnException_OnNegativeValues()
        {
            var options = new ServerComponentRunnerOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.MaxRetryAttempts = -1);
        }

        [Fact]
        public void ShutdownTimeout_Set_ThrowsAnException_OnNegativeValues()
        {
            var options = new ServerComponentRunnerOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.ShutdownTimeout = TimeSpan.FromSeconds(-1));
        }
    }
}
