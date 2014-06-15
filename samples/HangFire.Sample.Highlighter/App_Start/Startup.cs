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

            app.UseHangFire(config =>
            {
                config.UseAuthorizationFilters();
                config.UseSqlServerStorage("HighlighterDb");
                config.UseServer();
            });
        }
    }
}