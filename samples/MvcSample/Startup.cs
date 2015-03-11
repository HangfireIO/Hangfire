using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Pages;
using Hangfire.SqlServer;
using Hangfire.SqlServer.Msmq;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MvcSample.Startup))]

namespace MvcSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseHangfire(config =>
            {
                config.UseAuthorizationFilters();

                DashboardMetrics.Add(SqlServerStorage.ActiveConnections);
                DashboardPage.Metrics.Add(SqlServerStorage.ActiveConnections);
                DashboardMetrics.Add(SqlServerStorage.TotalConnections);
                DashboardPage.Metrics.Add(SqlServerStorage.TotalConnections);

                config
                    .UseSqlServerStorage(@"Server=.\sqlexpress;Database=Hangfire.Sample;Trusted_Connection=True;")
                    .UseMsmqQueues(@".\Private$\hangfire{0}", "default", "critical");
            });
        }
    }
}
