using System.Collections.Generic;
using Hangfire.Dashboard;
using Moq;
using Xunit;
using Hangfire;

namespace Hangfire.Core.Tests.Dashboard
{
    public class DashboardPermissionsFacts
    {
        [Fact]
        public void IsAuthorized_True_ByDefault()
        {
            var contextMock = new Mock<IDashboardContext>();
            contextMock.Setup(c => c.Options).Returns(new DashboardOptions());
            var permissions = new DashboardPermissionsContext(contextMock.Object);
            Assert.Equal(true, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsAuthorized_True_WhenAuthorized()
        {
            var options = new DashboardOptions
            {
                Permissions = new Dictionary<DashboardPermission, IEnumerable<IDashboardAuthorizationFilter>>
                {
                    [DashboardPermission.DeleteJob] = new []{ new AuthorizedDashboardFilter() }
                }
            };
            var contextMock = new Mock<IDashboardContext>();
            contextMock.Setup(c => c.Options).Returns(options);
            var permissions = new DashboardPermissionsContext(contextMock.Object);
            Assert.Equal(true, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsAuthorized_False_WhenUnauthorized()
        {
            var options = new DashboardOptions
            {
                Permissions = new Dictionary<DashboardPermission, IEnumerable<IDashboardAuthorizationFilter>>
                {
                    [DashboardPermission.DeleteJob] = new[] { new UnauthorizedDashboardFilter() }
                }
            };
            var contextMock = new Mock<IDashboardContext>();
            contextMock.Setup(c => c.Options).Returns(options);
            var permissions = new DashboardPermissionsContext(contextMock.Object);
            Assert.Equal(false, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsAuthorized_False_WhenMixed()
        {
            var options = new DashboardOptions
            {
                Permissions = new Dictionary<DashboardPermission, IEnumerable<IDashboardAuthorizationFilter>>
                {
                    [DashboardPermission.DeleteJob] = new IDashboardAuthorizationFilter[]
                    {
                        new UnauthorizedDashboardFilter(),
                        new AuthorizedDashboardFilter()
                    }
                }
            };
            var contextMock = new Mock<IDashboardContext>();
            contextMock.Setup(c => c.Options).Returns(options);
            var permissions = new DashboardPermissionsContext(contextMock.Object);
            Assert.Equal(false, permissions.IsAuthorized(DashboardPermission.DeleteJob));
        }

        [Fact]
        public void IsReadOnly_False_ByDefault()
        {
            var contextMock = new Mock<IDashboardContext>();
            contextMock.Setup(c => c.Options).Returns(new DashboardOptions());
            var permissions = new DashboardPermissionsContext(contextMock.Object);
            Assert.Equal(false, permissions.IsReadOnly);
        }

        [Fact]
        public void IsReadOnly_False_WhenOneAuthorized()
        {
            var options = new DashboardOptions
            {
                Permissions = new Dictionary<DashboardPermission, IEnumerable<IDashboardAuthorizationFilter>>
                {
                    [DashboardPermission.DeleteJob] = new[] { new AuthorizedDashboardFilter() },
                    [DashboardPermission.EnqueueJob] = new[] { new UnauthorizedDashboardFilter() }
                }
            };
            var contextMock = new Mock<IDashboardContext>();
            contextMock.Setup(c => c.Options).Returns(options);
            var permissions = new DashboardPermissionsContext(contextMock.Object);
            Assert.Equal(false, permissions.IsReadOnly);
        }

        [Fact]
        public void IsReadOnly_True_WhenAllUnauthorized()
        {
            var options = new DashboardOptions
            {
                Permissions = new Dictionary<DashboardPermission, IEnumerable<IDashboardAuthorizationFilter>>
                {
                    [DashboardPermission.DeleteJob] = new[] { new UnauthorizedDashboardFilter() },
                    [DashboardPermission.EnqueueJob] = new[] { new UnauthorizedDashboardFilter() }
                }
            };
            var contextMock = new Mock<IDashboardContext>();
            contextMock.Setup(c => c.Options).Returns(options);
            var permissions = new DashboardPermissionsContext(contextMock.Object);
            Assert.Equal(true, permissions.IsReadOnly);
        }
    }
}
