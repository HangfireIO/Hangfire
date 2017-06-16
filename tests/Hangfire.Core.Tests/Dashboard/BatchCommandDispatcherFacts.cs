using System.Collections.Generic;
using Hangfire.Dashboard;
using Moq;
using Xunit;
using Hangfire;

namespace Hangfire.Core.Tests.Dashboard
{
    public class BatchCommandDispatcherFacts
    {
        [Fact]
        public void Dispatch_Sets422StatusCode_WhenNotPermitted()
        {
            var options = new DashboardOptions
            {
                Permissions = new Dictionary<DashboardPermission, IEnumerable<IDashboardAuthorizationFilter>>
                {
                    [DashboardPermission.DeleteJob] = new[] { new UnauthorizedDashboardFilter() }
                }
            };
            var contextMock = new Mock<IDashboardContext>();
            contextMock.SetupAllProperties();
            contextMock.Setup(c => c.Options).Returns(options);
            contextMock.Setup(c => c.Permissions).Returns(new DashboardPermissionsContext(contextMock.Object));
            var responseMock = new Mock<DashboardResponse>();
            responseMock.SetupAllProperties();
            contextMock.Setup(c => c.Response).Returns(responseMock.Object);

            var dispatcher = new BatchCommandDispatcher((context, str) => { }, DashboardPermission.DeleteJob);
            dispatcher.Dispatch(contextMock.Object);
            Assert.Equal(422, contextMock.Object.Response.StatusCode);
        }
    }
}
