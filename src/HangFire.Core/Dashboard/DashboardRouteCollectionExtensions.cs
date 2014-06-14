using System;
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
    }
}
