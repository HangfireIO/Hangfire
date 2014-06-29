using System;
using Hangfire.Server;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ServerSupervisorOptionsFacts
    {
        [Fact]
        public void MaxRetryAttempts_Set_ThrowsAnException_OnNegativeValues()
        {
            var options = new ServerSupervisorOptions();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => options.MaxRetryAttempts = -1);
        }
    }
}
