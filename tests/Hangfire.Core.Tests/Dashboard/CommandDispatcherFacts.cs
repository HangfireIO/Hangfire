using System.Collections.Generic;
using Hangfire.Dashboard;
using Moq;
using Xunit;
using Hangfire;

namespace Hangfire.Core.Tests.Dashboard
{
    public class CommandDispatcherFacts
    {
        [Fact]
        public void Dispatch_Sets422StatusCode_WhenNotPermitted()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new[] { new UnauthorizedDashboardFilter() }
            };
            var context = new TestDashboardContext(options);
            var dispatcher = new CommandDispatcher(_ => false, DashboardPermission.DeleteJob);
            dispatcher.Dispatch(context);
            Assert.Equal(422, context.Response.StatusCode);
        }
    }
}
