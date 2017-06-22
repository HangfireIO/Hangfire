using Hangfire.Dashboard;

namespace Hangfire.Core.Tests.Stubs
{
    internal class UnauthorizedDashboardFilterStub : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return false;
        }
    }
}
