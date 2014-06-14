using HangFire.Dashboard.Pages;

namespace HangFire.Dashboard
{
    public static class GlobalDashboardRoutes
    {
        static GlobalDashboardRoutes()
        {
            Routes = new DashboardRouteCollection();
            Routes.AddRazorPage("/", x => new DashboardPage());
        }

        public static DashboardRouteCollection Routes { get; private set; }
    }
}
