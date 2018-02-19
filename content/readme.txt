Please see http://docs.hangfire.io for more information on using Hangfire. The
`Hangfire` package is using SQL Server as a job storage and intended to run in
any OWIN-based web application.

+-----------------------------------------------------------------------------+
|  !!! DASHBOARD REQUIRES AUTH CONFIGURATION !!!                              |
+-----------------------------------------------------------------------------+

By default, ONLY LOCAL requests are allowed to access the Dashboard. Please
see the `Configuring Dashboard authorization` section in Hangfire documentation:
http://docs.hangfire.io/en/latest/configuration/using-dashboard.html#configuring-authorization

Sample ASP.NET Core Startup class
---------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;

namespace MyWebApplication
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(x => x.UseSqlServerStorage("<connection string>"));
        }
        
        public void Configure(IApplicationBuilder app)
        {
            app.UseHangfireServer();
            app.UseHangfireDashboard();
        }
    }
}


Sample OWIN Startup class
-------------------------

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
