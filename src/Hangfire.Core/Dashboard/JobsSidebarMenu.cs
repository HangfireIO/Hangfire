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
    public static class JobsSidebarMenu
    {
        public static readonly List<Func<RazorPage, MenuItem>> Items
            = new List<Func<RazorPage, MenuItem>>();

        static JobsSidebarMenu()
        {
            Items.Add(page => new MenuItem(Strings.JobsSidebarMenu_Enqueued, page.Url.LinkToQueues())
            {
                Active = page.RequestPath.StartsWith("/jobs/enqueued"),
                Metric = DashboardMetrics.EnqueuedAndQueueCount
            });

            Items.Add(page => new MenuItem(Strings.JobsSidebarMenu_Scheduled, page.Url.To("/jobs/scheduled"))
            {
                Active = page.RequestPath.StartsWith("/jobs/scheduled"),
                Metric = DashboardMetrics.ScheduledCount
            });

            Items.Add(page => new MenuItem(Strings.JobsSidebarMenu_Processing, page.Url.To("/jobs/processing"))
            {
                Active = page.RequestPath.StartsWith("/jobs/processing"),
                Metric = DashboardMetrics.ProcessingCount
            });

            Items.Add(page => new MenuItem(Strings.JobsSidebarMenu_Succeeded, page.Url.To("/jobs/succeeded"))
            {
                Active = page.RequestPath.StartsWith("/jobs/succeeded"),
                Metric = DashboardMetrics.SucceededCount
            });

            Items.Add(page => new MenuItem(Strings.JobsSidebarMenu_Failed, page.Url.To("/jobs/failed"))
            {
                Active = page.RequestPath.StartsWith("/jobs/failed"),
                Metric = DashboardMetrics.FailedCount
            });

            Items.Add(page => new MenuItem(Strings.JobsSidebarMenu_Deleted, page.Url.To("/jobs/deleted"))
            {
                Active = page.RequestPath.StartsWith("/jobs/deleted"),
                Metric = DashboardMetrics.DeletedCount
            });

            Items.Add(page => new MenuItem(Strings.JobsSidebarMenu_Awaiting, page.Url.To("/jobs/awaiting"))
            {
                Active = page.RequestPath.StartsWith("/jobs/awaiting"),
                Metric = DashboardMetrics.AwaitingCount
            });
        }
    }
}