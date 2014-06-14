using System;
using HangFire.Dashboard.Pages;

namespace HangFire.Dashboard
{
    public static class GlobalDashboardRoutes
    {
        private static readonly string[] Javascripts =
        {
            "jquery-1.10.2.min.js", 
            "bootstrap.min.js",
            "moment.min.js",
            "d3.min.js", 
            "d3.layout.min.js", 
            "rickshaw.min.js", 
            "hangfire.js",
        };

        private static readonly string[] Stylesheets =
        {
            "bootstrap.min.css", 
            "rickshaw.min.css", 
            "hangfire.css",
        };

        static GlobalDashboardRoutes()
        {
            Routes = new DashboardRouteCollection();
            Routes.AddRazorPage("/", x => new DashboardPage());

            Routes.Add("/js", new CombinedResourceDispatcher(
                "application/javascript", 
                typeof(GlobalDashboardRoutes).Assembly,
                GetContentFolderNamespace("js"),
                Javascripts));

            Routes.Add("/css", new CombinedResourceDispatcher(
                "text/css",
                typeof(GlobalDashboardRoutes).Assembly,
                GetContentFolderNamespace("css"),
                Stylesheets));
        }

        public static DashboardRouteCollection Routes { get; private set; }

        internal static string GetContentFolderNamespace(string contentFolder)
        {
            return String.Format("{0}.Content.{1}", typeof(GlobalDashboardRoutes).Namespace, contentFolder);
        }
    }
}
