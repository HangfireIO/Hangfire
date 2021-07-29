using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HangFire.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();


            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseStaticFiles();


            //DashboardOptions options = new DashboardOptions();
            //options.Authorization = new[] { new DashboardNoAuthorizationFilter() };

            //app.UseAuthentication();
            app.UseHangfireDashboard();

            NavigationMenu.Items.Add(page => new MenuItem("Refresh", "/refresh"));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                //endpoints.MapControllerRoute(
                //  name: "default",
                //  pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            TesteJob t = new TesteJob();
            TesteJob2 t2 = new TesteJob2();

            RecurringJob.AddOrUpdate("Teste01",
              methodCall: (Expression<Action>)(() => t.Run()),
              "*/1 * * * *",
              timeZone: TimeZoneInfo.Utc,
              queue: $"job_110");

            RecurringJob.AddOrUpdate("Teste02",
                methodCall: (Expression<Action>)(() => t2.Run()),
                "*/1 * * * *",
                timeZone: TimeZoneInfo.Utc,
                queue: $"job_111");
        }
    }
}
