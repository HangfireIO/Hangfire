using Hangfire.Core.Tests.Stubs;
using Hangfire.Dashboard;
using Xunit;

namespace Hangfire.Core.Tests.Dashboard
{
    public class CommandDispatcherFacts
    {
        [Fact]
        public void Dispatch_Sets401StatusCode_WhenNotPermitted()
        {
            var options = new DashboardOptions
            {
                IsReadOnlyFunc = _ => true
            };
            var context = new DashboardContextStub(options);
            var dispatcher = new CommandDispatcher((DashboardContext _) => false);
            dispatcher.Dispatch(context);
            Assert.Equal(401, context.Response.StatusCode);
        }
    }
}
