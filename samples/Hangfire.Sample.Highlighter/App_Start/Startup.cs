using Hangfire.Sample.Highlighter;
using Hangfire.SqlServer;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace Hangfire.Sample.Highlighter
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();

            app.UseHangfire(config =>
            {
                config.UseAuthorizationFilters();
                config.UseSqlServerStorage("HighlighterDb");
                config.UseServer();
            });
        }
    }
}