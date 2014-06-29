using Hangfire;
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

                config
                    .UseSqlServerStorage(@"Server=.\sqlexpress;Database=Hangfire.Sample;Trusted_Connection=True;")
                    .UseMsmqQueues(@".\Private$\hangfire{0}", "default", "critical");
            });
        }
    }
}
