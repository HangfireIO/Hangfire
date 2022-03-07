// This file is part of Hangfire. Copyright © 2015 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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