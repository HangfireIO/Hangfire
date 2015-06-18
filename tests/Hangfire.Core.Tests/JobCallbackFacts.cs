using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class JobCallbackFacts
    {
        [Fact]
        public void Log_ActuallyLogs()
        {
            Assert.Null(JobCancellationToken.Null);
        }

        [Fact]
        public void Null_ReturnsNullValue()
        {
            Assert.Null(JobCallback.Null);
        }
    }
}
