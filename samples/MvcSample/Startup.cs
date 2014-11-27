using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Oracle;
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
                //config.UseOracleStorage("Data Source=//localhost:1521/XE;User Id=hangfire;Password=hangfire;");
                config.UseSqlServerStorage(@"Server=.\sqlexpress;Database=Hangfire.Sample;Trusted_Connection=True;");
                //.UseMsmqQueues(@".\Private$\hangfire{0}", "default", "critical");
            });
        }
    }
}
