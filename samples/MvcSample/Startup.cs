using HangFire;
using HangFire.SqlServer;
using HangFire.SqlServer.Msmq;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MvcSample.Startup))]

namespace MvcSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseHangFire(config => config
                .UseSqlServerStorage(@"Server=.\sqlexpress;Database=HangFire.Sample;Trusted_Connection=True;")
                .UseMsmqQueues(@".\Private$\hangfire{0}", "default", "critical"));
        }
    }
}
