using System;
using Hangfire.Dashboard;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Dashboard
{
    public class DashboardPermissionsFacts
    {
        [Fact]
        public void IsReadOnly_TrueWhenNoPermissions()
        {
            var permissions = new DashboardPermissions
            {
                CanDelete = false,
                CanTrigger = false
            };
            Assert.Equal(true, permissions.IsReadOnly);
        }

        [Fact]
        public void IsReadOnly_FalseWhenHasAtLeastOnePermission()
        {
            var permissions = new DashboardPermissions
            {
                CanDelete = true
            };
            Assert.Equal(false, permissions.IsReadOnly);
        }
    }
}
