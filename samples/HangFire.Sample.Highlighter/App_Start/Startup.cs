using System.Configuration;
using System.Threading;
using HangFire.Dashboard;
using HangFire.Sample.Highlighter;
using HangFire.SqlServer;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace HangFire.Sample.Highlighter
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
            app.Map("/hangfire", subApp => subApp.Use<DashboardMiddleware>(GlobalDashboardRoutes.Routes));

            JobStorage.Current = new SqlServerStorage("HighlighterDb");

            var server = new BackgroundJobServer();

            var context = new OwinContext(app.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");
            if (token != CancellationToken.None)
            {
                token.Register(server.Stop);
            }
        }
    }
}