Please see http://docs.hangfire.io for more information on using HangFire. The
`HangFire` package is using SQL Server as a job storage and intended to run in
any OWIN-based web application.

Bootstrapping HangFire
-----------------------
To run HangFire in your application, update the existing OWIN Startup class or
create the file `Startup.cs` in the root foloder of your application as shown
below. Please see also http://docs.hangfire.io/en/latest/quickstart.html.

using HangFire;
using HangFire.SqlServer;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MyWebApplication.Startup))]

namespace MyWebApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseHangFire(config =>
            {
                config.UseSqlServerStorage("<name or connection string>");
                config.UseServer();
            });
        }
    }
}
