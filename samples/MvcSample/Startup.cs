using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.Server;
using Hangfire.SqlServer;
using Microsoft.Owin;
using MvcSample;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

[assembly: OwinStartup(typeof(Startup))]

namespace MvcSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                .UseSqlServerStorage(@"Server=.\sqlexpress;Database=Hangfire.Sample;Trusted_Connection=True;")
                .UseMsmqQueues(@".\Private$\hangfire{0}", "default", "critical")
                .UseDashboardMetric(SqlServerStorage.ActiveConnections)
                .UseDashboardMetric(SqlServerStorage.TotalConnections)
                .UseDashboardMetric(DashboardMetrics.FailedCount);
            GlobalJobFilters.Filters.Add(new TestAttribute());

            app.UseHangfireDashboard();

            var jobsInitialParams = new Dictionary<string, object>();
            jobsInitialParams.Add("Key", Guid.NewGuid());
            jobsInitialParams.Add("Id", new Random(1).Next(1,5));

            RecurringJob.AddOrUpdate(() => this.WriteLine("Testing Params on job creation"), Cron.Minutely, null, "default",
                jobsInitialParams);
        }

        public void WriteLine(string s)
        {
            Console.WriteLine(s);
        }
    }


    public class TestAttribute : JobFilterAttribute, IServerFilter, IClientFilter, IServerExceptionFilter
    {
        public void OnCreating(CreatingContext context)
        {
            var key = context.GetJobParameter<Guid>("Key");
            var id = context.GetJobParameter<int>("Id");
        }

        public void OnPerforming(PerformingContext context)
        {
        }

        void IClientFilter.OnCreated(CreatedContext filterContext)
        {
        }

        void IServerFilter.OnPerformed(PerformedContext context)
        {
        }

        public void OnServerException(ServerExceptionContext filterContext)
        {
        }
    }
}
