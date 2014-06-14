using HangFire.Dashboard;
using HangFire.Sample.Highlighter;
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
            app.Map("~/hangfire", subApp => subApp.Use<DashboardMiddleware>());
        }
    }
}