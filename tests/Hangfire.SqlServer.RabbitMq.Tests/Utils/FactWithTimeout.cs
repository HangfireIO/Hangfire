using System;
using System.Diagnostics;
using Xunit;

namespace Hangfire.SqlServer.RabbitMq.Tests
{
    internal class FactWithTimeout : FactAttribute
    {
        public FactWithTimeout()
        {
            Timeout = Debugger.IsAttached ? Int32.MaxValue : 30 * 1000;
        }
    }
}
