using System;
using System.IO;
using System.Text.RegularExpressions;
using HangFire.Dashboard.Pages;

namespace HangFire.Dashboard
{
    public static class DashboardRouteCollectionExtensions
    {
        public static void AddRazorPage(
            this DashboardRouteCollection routes, 
            string pathTemplate, 
            Func<Match, RazorPage> pageFunc)
        {
            routes.Add(pathTemplate, new RazorPageDispatcher(pageFunc));
        }

        public static void AddCommand(
            this DashboardRouteCollection routes,
            string pathTemplate,
            Func<Match, bool> command)
        {
            routes.Add(pathTemplate, new CommandDispatcher(command));
        }

        public static void AddBatchCommand(
            this DashboardRouteCollection routes,
            string pathTemplate,
            Action<string> command)
        {
            routes.Add(pathTemplate, new BatchCommandDispatcher(command));
        }
    }
}
