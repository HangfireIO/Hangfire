using System.Linq;
using Hangfire.Dashboard;
using Xunit;

namespace Hangfire.Core.Tests.Dashboard
{
    public class DashboardOptionsFacts
    {
        [Fact]
        public void Ctor_SetsDefaultValues_ForAllOptions()
        {
            var options = new DashboardOptions();
            Assert.Equal("/", options.AppPath);
            Assert.Equal("", options.PrefixPath);
            Assert.NotNull(options.Authorization);
            Assert.IsType<LocalRequestsOnlyAuthorizationFilter>(options.Authorization.FirstOrDefault());
            Assert.Equal(2000, options.StatsPollingInterval);
            Assert.True(options.DisplayStorageConnectionString);
            Assert.Equal("Hangfire Dashboard", options.DashboardTitle);
        }
    }
}
