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