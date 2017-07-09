using Hangfire.Dashboard;

namespace Hangfire.Core.Tests.Stubs
{
    internal class TestAuthorizedDashboardFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}
