using System.Data.Entity;
using System.Web.Mvc;
using System.Web.Routing;
using Hangfire.Sample.Highlighter.Migrations;
using Hangfire.Sample.Highlighter.Models;
using StackExchange.Profiling;
using StackExchange.Profiling.EntityFramework6;

namespace Hangfire.Sample.Highlighter
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            MiniProfilerEF6.Initialize();
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<HighlighterDbContext, Configuration>());
        }

        protected void Application_BeginRequest()
        {
            MiniProfiler.Start();
        }

        protected void Application_EndRequest()
        {
            MiniProfiler.Stop();
        }
    }
}
