using System.Text.RegularExpressions;

namespace Hangfire.Dashboard
{
    public interface IDashboardContext
    {
        DashboardOptions Options { get; }
        DashboardRequest Request { get; }
        DashboardResponse Response { get; }
        DashboardPermissionsContext Permissions { get; }
        JobStorage Storage { get; }
        Match UriMatch { get; set; }
    }
}