using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;

namespace AspNetCore30Sample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton<SqlServerStorageOptions>(new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromTicks(1),
                UseRecommendedIsolationLevel = true,
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(1)
            });

            services.AddHangfire((provider, configuration) => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseSqlServerStorage(
                    @"Data Source=(LocalDB)\v13.0;Initial Catalog=Hangfire.Sample;Integrated Security=True;",
                    provider.GetRequiredService<SqlServerStorageOptions>()));

            services.AddHangfireServer(options =>
            {
                options.StopTimeout = TimeSpan.FromSeconds(15);
                options.ShutdownTimeout = TimeSpan.FromSeconds(30);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // Option 1
            app.UseHangfireDashboard();

            app.UseEndpoints(endpoints =>
            {
                // Option 2
                //endpoints.MapHangfireDashboard();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("<html><body><p>Hello World! <a href=\"/hangfire\">Hangfire Dashboard</a></p></body></html>");
                });
            });
        }
    }
}
