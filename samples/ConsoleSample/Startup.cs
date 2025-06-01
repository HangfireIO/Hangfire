using System;
using Hangfire;
using Microsoft.Owin.Diagnostics;
using Owin;

namespace ConsoleSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage(ErrorPageOptions.ShowAll);
            app.UseHangfireDashboard(String.Empty);

            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                Queues = new[] { "critical", "default" },
                TaskScheduler = null,
                SchedulePollingInterval = TimeSpan.FromSeconds(1)
            });
        }
    }
}