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
            app.MapHangFireDashboard();

            var storage = new SqlServerStorage(
                @"Server=.\sqlexpress;Database=HangFire.Sample;Trusted_Connection=True;");
            storage.UseMsmqQueues(@".\Private$\hangfire{0}", "default", "critical");
        }
    }
}
