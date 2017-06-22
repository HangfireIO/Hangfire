using System.Collections.Generic;
using Hangfire.Dashboard;
using Moq;
using Xunit;
using Hangfire;

namespace Hangfire.Core.Tests.Dashboard
{
    public class DashboardAuthorizationContextFacts
    {
        [Fact]
        public void IsAuthorized_True_ByDefault()
        {
            var context = new TestDashboardContext(new DashboardOptions());
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(true, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsAuthorized_True_WhenAuthorized()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new[] { new AuthorizedDashboardFilter() }
            };
            var context = new TestDashboardContext(options);
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(true, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsAuthorized_False_WhenUnauthorized()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new[] { new UnauthorizedDashboardFilter() }
            };
            var context = new TestDashboardContext(options);
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
                    new UnauthorizedDashboardFilter(),
                    new AuthorizedDashboardFilter()
                }
            };
            var context = new TestDashboardContext(options);
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(false, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsReadOnly_False_ByDefault()
        {
            var context = new TestDashboardContext(new DashboardOptions());
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(false, permissions.IsReadOnly);
        }

        [Fact]
        public void IsReadOnly_False_WhenOneAuthorized()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new[] { new AuthorizedDashboardFilter() },
                EnqueueJobAuthorization = new[] { new UnauthorizedDashboardFilter() }
            };
            var context = new TestDashboardContext(options);
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(false, permissions.IsReadOnly);
        }

        [Fact]
        public void IsReadOnly_True_WhenAllUnauthorized()
        {
            var options = new DashboardOptions
            {
                DeleteJobAuthorization = new[] { new UnauthorizedDashboardFilter() },
                EnqueueJobAuthorization = new[] { new UnauthorizedDashboardFilter() }
            };
            var context = new TestDashboardContext(options);
            var permissions = new DashboardAuthorizationContext(context);
            Assert.Equal(true, permissions.IsReadOnly);
        }
    }
}
