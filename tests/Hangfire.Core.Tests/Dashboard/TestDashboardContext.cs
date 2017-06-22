using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.SqlServer;

namespace Hangfire.Core.Tests.Dashboard
{
    class TestDashboardContext : DashboardContext
    {
        public TestDashboardContext(DashboardOptions options) : base(new TestJobStorage(), options)
        {
            Response = new TestResponse();
        }
    }
}
