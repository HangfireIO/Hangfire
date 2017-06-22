using Hangfire.Core.Tests.Stubs;
using Hangfire.Dashboard;
using Xunit;

namespace Hangfire.Core.Tests.Dashboard
{
    public class BatchCommandDispatcherFacts
    {
        [Fact]
        public void Dispatch_Sets422StatusCode_WhenNotPermitted()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new[] { new UnauthorizedDashboardFilterStub() }
            };
            var context = new DashboardContextStub(options);
            var dispatcher = new BatchCommandDispatcher((ctx, str) => { }, DashboardPermission.DeleteJob);
            dispatcher.Dispatch(context);
            Assert.Equal(422, context.Response.StatusCode);
        }
    }
}
