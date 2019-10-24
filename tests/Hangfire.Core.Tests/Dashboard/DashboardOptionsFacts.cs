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
            Assert.Equal(options.AppPath, "/");
            Assert.NotNull(options.Authorization);
            Assert.Equal(options.StatsPollingInterval, 2000);
            Assert.Equal(options.DisplayStorageConnectionString,true);
            Assert.Equal(options.DashboardTitle, "Hangfire Dashboard");
        }
    }
}
