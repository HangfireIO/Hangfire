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
    public static class NavigationMenu
    {
        public static readonly List<Func<RazorPage, MenuItem>> Items = new List<Func<RazorPage, MenuItem>>();

        static NavigationMenu()
        {
            Items.Add(page => new MenuItem("Jobs", page.LinkTo("/jobs/enqueued"))
            {
                Active = page.RequestPath.StartsWith("/jobs"),
                Metric = page.Statistics.Failed != 0
                    ? new Metric(page.Statistics.Failed)
                    {
                        Style = MetricStyle.Danger,
                        Highlighted = true
                    }
                    : new Metric(page.Statistics.Enqueued)
                    {
                        Style = MetricStyle.Success
                    }
            });

            Items.Add(page => new MenuItem("Retries", "#")
            {
                Metric = new Metric(12) { Style = MetricStyle.Warning }
            });

            Items.Add(page => new MenuItem("Recurring", page.LinkTo("/recurring"))
            {
                Active = page.RequestPath.StartsWith("/recurring"),
                Metric = new Metric(page.Statistics.Recurring)
            });

            Items.Add(page => new MenuItem("Servers", page.LinkTo("/servers"))
            {
                Active = page.RequestPath.Equals("/servers"),
                Metric = new Metric(page.Statistics.Servers)
                {
                    Style = page.Statistics.Servers == 0 ? MetricStyle.Warning : MetricStyle.None,
                    Highlighted = page.Statistics.Servers == 0
                }
            });
        }
    }
}