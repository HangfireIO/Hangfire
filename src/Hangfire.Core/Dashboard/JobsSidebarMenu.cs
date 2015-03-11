// This file is part of Hangfire.
// Copyright © 2015 Sergey Odinokov.
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

namespace Hangfire.Dashboard
{
    public static class JobsSidebarMenu
    {
        public static readonly List<Func<RazorPage, MenuItem>> Items
            = new List<Func<RazorPage, MenuItem>>();

        static JobsSidebarMenu()
        {
            Items.Add(page => new MenuItem("Enqueued", page.LinkTo("/jobs/enqueued"))
            {
                Active = page.RequestPath.StartsWith("/jobs/enqueued"),
                Metric = DashboardMetrics.EnqueuedAndQueueCount
            });

            Items.Add(page => new MenuItem("Scheduled", page.LinkTo("/jobs/scheduled"))
            {
                Active = page.RequestPath.StartsWith("/jobs/scheduled"),
                Metric = DashboardMetrics.ScheduledCount
            });

            Items.Add(page => new MenuItem("Processing", page.LinkTo("/jobs/processing"))
            {
                Active = page.RequestPath.StartsWith("/jobs/processing"),
                Metric = DashboardMetrics.ProcessingCount
            });

            Items.Add(page => new MenuItem("Succeeded", page.LinkTo("/jobs/succeeded"))
            {
                Active = page.RequestPath.StartsWith("/jobs/succeeded"),
                Metric = DashboardMetrics.SucceededCount
            });

            Items.Add(page => new MenuItem("Failed", page.LinkTo("/jobs/failed"))
            {
                Active = page.RequestPath.StartsWith("/jobs/failed"),
                Metric = DashboardMetrics.FailedCount
            });

            Items.Add(page => new MenuItem("Deleted", page.LinkTo("/jobs/deleted"))
            {
                Active = page.RequestPath.StartsWith("/jobs/deleted"),
                Metric = DashboardMetrics.DeletedCount
            });
        }
    }
}