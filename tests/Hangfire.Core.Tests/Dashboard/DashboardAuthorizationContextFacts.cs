using Hangfire.Core.Tests.Stubs;
using Hangfire.Dashboard;
using Xunit;

namespace Hangfire.Core.Tests.Dashboard
{
    public class DashboardAuthorizationContextFacts
    {
        [Fact]
        public void IsAuthorized_True_ByDefault()
        {
            var context = new DashboardContextStub(new DashboardOptions());
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(true, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsAuthorized_True_WhenAuthorized()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new[] { new TestAuthorizedDashboardFilter() }
            };
            var context = new DashboardContextStub(options);
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(true, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsAuthorized_False_WhenUnauthorized()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new[] { new UnauthorizedDashboardFilterStub() }
            };
            var context = new DashboardContextStub(options);
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(false, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsAuthorized_False_WhenMixed()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new IDashboardAuthorizationFilter[]
                {
                    new UnauthorizedDashboardFilterStub(),
                    new TestAuthorizedDashboardFilter()
                }
            };
            var context = new DashboardContextStub(options);
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(false, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsReadOnly_False_ByDefault()
        {
            var context = new DashboardContextStub(new DashboardOptions());
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(false, permissions.IsReadOnly);
        }

        [Fact]
        public void IsReadOnly_False_WhenOneAuthorized()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new[] { new TestAuthorizedDashboardFilter() },
                EnqueueJobAuthorization = new[] { new UnauthorizedDashboardFilterStub() }
            };
            var context = new DashboardContextStub(options);
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(false, permissions.IsReadOnly);
        }

        [Fact]
        public void IsReadOnly_True_WhenAllUnauthorized()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new[] { new UnauthorizedDashboardFilterStub() },
                EnqueueJobAuthorization = new[] { new UnauthorizedDashboardFilterStub() }
            };
            var context = new DashboardContextStub(options);
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(true, permissions.IsReadOnly);
        }
    }
}
