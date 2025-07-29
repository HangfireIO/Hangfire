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
using Hangfire.Annotations;
using Hangfire.Dashboard.Resources;

namespace Hangfire.Dashboard
{
    public static class JobsSidebarMenu
    {
        [NotNull]
        public static readonly List<Func<RazorPage, MenuItem>> Items = new List<Func<RazorPage, MenuItem>>();

        static JobsSidebarMenu()
        {
            Items.Add(static page => new MenuItem(Strings.JobsSidebarMenu_Enqueued, page.Url.LinkToQueues())
            {
                Active = page.RequestPath?.StartsWith("/jobs/enqueued", StringComparison.OrdinalIgnoreCase) ?? false,
                Metric = DashboardMetrics.EnqueuedAndQueueCount
            });

            Items.Add(static page => new MenuItem(Strings.JobsSidebarMenu_Scheduled, page.Url.To("/jobs/scheduled"))
            {
                Active = page.RequestPath?.StartsWith("/jobs/scheduled", StringComparison.OrdinalIgnoreCase) ?? false,
                Metric = DashboardMetrics.ScheduledCount
            });

            Items.Add(static page => new MenuItem(Strings.JobsSidebarMenu_Processing, page.Url.To("/jobs/processing"))
            {
                Active = page.RequestPath?.StartsWith("/jobs/processing", StringComparison.OrdinalIgnoreCase) ?? false,
                Metric = DashboardMetrics.ProcessingCount
            });

            Items.Add(static page => new MenuItem(Strings.JobsSidebarMenu_Succeeded, page.Url.To("/jobs/succeeded"))
            {
                Active = page.RequestPath?.StartsWith("/jobs/succeeded", StringComparison.OrdinalIgnoreCase) ?? false,
                Metric = DashboardMetrics.SucceededCount
            });

            Items.Add(static page => new MenuItem(Strings.JobsSidebarMenu_Failed, page.Url.To("/jobs/failed"))
            {
                Active = page.RequestPath?.StartsWith("/jobs/failed", StringComparison.OrdinalIgnoreCase) ?? false,
                Metric = DashboardMetrics.FailedCount
            });

            Items.Add(static page => new MenuItem(Strings.JobsSidebarMenu_Deleted, page.Url.To("/jobs/deleted"))
            {
                Active = page.RequestPath?.StartsWith("/jobs/deleted", StringComparison.OrdinalIgnoreCase) ?? false,
                Metric = DashboardMetrics.DeletedCount
            });

            Items.Add(static page => new MenuItem(Strings.JobsSidebarMenu_Awaiting, page.Url.To("/jobs/awaiting"))
            {
                Active = page.RequestPath?.StartsWith("/jobs/awaiting", StringComparison.OrdinalIgnoreCase) ?? false,
                Metric = DashboardMetrics.AwaitingCount
            });
        }
    }
}