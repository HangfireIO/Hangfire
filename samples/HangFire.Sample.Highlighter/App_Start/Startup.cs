using System.Threading;
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
            app.MapHangFireDashboard();

            JobStorage.Current = new SqlServerStorage("HighlighterDb");

            var server = new BackgroundJobServer();
            server.Start();

            var context = new OwinContext(app.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");
            if (token != CancellationToken.None)
            {
                token.Register(server.Stop);
            }
        }
    }
}