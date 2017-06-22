using System;
using System.IO;
using System.Threading.Tasks;
using Hangfire.Dashboard;

namespace Hangfire.Core.Tests.Stubs
{
    class DashboardResponseStub : DashboardResponse
    {
        public override string ContentType { get; set; }
        public override int StatusCode { get; set; }
        public override Stream Body { get; }
        public override void SetExpire(DateTimeOffset? value)
        {
            throw new NotImplementedException();
        }

        public override Task WriteAsync(string text)
        {
            throw new NotImplementedException();
        }
    }
}