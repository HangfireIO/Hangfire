Please see http://docs.hangfire.io for more information on using Hangfire. The
`Hangfire` package is using SQL Server as a job storage and intended to run in
any OWIN-based web application.

To run Hangfire in your application, update the existing OWIN Startup class or
create the file `Startup.cs` in the root folder of your application as shown
below. Please see also http://docs.hangfire.io/en/latest/quickstart.html.

!!! DASHBOARD REQUIRES AUTH CONFIGURATION !!!

By default, ONLY LOCAL requests are allowed to access the Dashboard. Please
see the `Configuring Dashboard authorization` section in Hangfire documentation:
http://docs.hangfire.io/en/latest/configuration/configuring-authorization.html

Sample OWIN Startup class
--------------------------

using Hangfire;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MyWebApplication.Startup))]

namespace MyWebApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
		    GlobalConfiguration.Configuration
			    .UseSqlServerStorage("<name or connection string>");

			app.UseHangfireDashboard();
			app.UseHangfireServer();
        }
    }
}
