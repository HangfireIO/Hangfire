// This file is part of Hangfire. Copyright © 2015 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Hangfire.Dashboard.Resources;

namespace Hangfire.Dashboard
{
    public static class NavigationMenu
    {
        public static readonly List<Func<RazorPage, MenuItem>> Items = new List<Func<RazorPage, MenuItem>>();

        static NavigationMenu()
        {
            Items.Add(page => new MenuItem(Strings.NavigationMenu_Jobs, page.Url.LinkToQueues())
            {
                Active = page.RequestPath.StartsWith("/jobs"),
                Metrics = new []
                {
                    DashboardMetrics.EnqueuedCountOrNull,
                    DashboardMetrics.FailedCountOrNull
                }
            });

            Items.Add(page => new MenuItem(Strings.NavigationMenu_Retries, page.Url.To("/retries"))
            {
                Active = page.RequestPath.StartsWith("/retries"),
                Metric = DashboardMetrics.RetriesCount
            });

            Items.Add(page => new MenuItem(Strings.NavigationMenu_RecurringJobs, page.Url.To("/recurring"))
            {
                Active = page.RequestPath.StartsWith("/recurring"),
                Metric = DashboardMetrics.RecurringJobCount
            });

            Items.Add(page => new MenuItem(Strings.NavigationMenu_Servers, page.Url.To("/servers"))
            {
                Active = page.RequestPath.Equals("/servers"),
                Metric = DashboardMetrics.ServerCount
            });
        }
    }
}