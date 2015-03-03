using System;
using System.Collections.Generic;

namespace Hangfire.Dashboard
{
    public enum MetricStyle
    {
        None,
        Info,
        Success,
        Warning,
        Danger
    }

    public static class MetricStyleExtensions
    {
        public static string ToClassName(this MetricStyle style)
        {
            switch (style)
            {
                case MetricStyle.Info:    return "metric-info";
                case MetricStyle.Success: return "metric-success";
                case MetricStyle.Warning: return "metric-warning";
                case MetricStyle.Danger:  return "metric-danger";
                default:                  return "metric-default";
            }
        }
    }

    public class DashboardMetric
    {
        public DashboardMetric(params long[] values)
        {
            Values = values;
        }

        public long[] Values { get; private set; }
        public MetricStyle Style { get; set; }
        public bool Highlighted { get; set; }
    }

    public class DashboardMenuItem
    {
        public string Text { get; set; }
        public string Url { get; set; }
        public bool Active { get; set; }
        
        public DashboardMetric Metric { get; set; }

        public double Order { get; set; }
    }

    public static class DashboardMenu
    {
        public static readonly List<Func<RazorPage, DashboardMenuItem>> Items
            = new List<Func<RazorPage, DashboardMenuItem>>();

        static DashboardMenu()
        {
            Items.Add(page => new DashboardMenuItem
            {
                Text = "Jobs",
                Url = page.LinkTo("/jobs/enqueued"),
                Active = page.RequestPath.StartsWith("/jobs"),
                Metric = page.Statistics.Failed != 0
                    ? new DashboardMetric(page.Statistics.Failed)
                    {
                        Style = MetricStyle.Danger,
                        Highlighted = true
                    }
                    : new DashboardMetric(page.Statistics.Enqueued)
                    {
                        Style = MetricStyle.Success
                    }
            });

            Items.Add(page => new DashboardMenuItem
            {
                Text = "Retries",
                Url = "#",
                Active = false,
                Metric = new DashboardMetric(12)
                {
                    Style = MetricStyle.Warning
                }
            });

            Items.Add(page => new DashboardMenuItem
            {
                Text = "Recurring",
                Url = page.LinkTo("/recurring"),
                Active = page.RequestPath.StartsWith("/recurring"),
                Metric = new DashboardMetric(page.Statistics.Recurring)
            });

            Items.Add(page => new DashboardMenuItem
            {
                Text = "Servers",
                Url = page.LinkTo("/servers"),
                Active = page.RequestPath.Equals("/servers"),
                Metric = new DashboardMetric(page.Statistics.Servers)
                {
                    Style = page.Statistics.Servers == 0 ? MetricStyle.Warning : MetricStyle.None,
                    Highlighted = page.Statistics.Servers == 0
                }
            });
        }
    }

    public static class JobsSidebarMenu
    {
        public static readonly List<Func<RazorPage, DashboardMenuItem>> Items
            = new List<Func<RazorPage, DashboardMenuItem>>();

        static JobsSidebarMenu()
        {
            Items.Add(page => new DashboardMenuItem
            {
                Text = "Enqueued",
                Url = page.LinkTo("/jobs/enqueued"),
                Active = page.RequestPath.StartsWith("/jobs/enqueued"),
                Metric = new DashboardMetric(page.Statistics.Enqueued, page.Statistics.Queues)
            });

            Items.Add(page => new DashboardMenuItem
            {
                Text = "Scheduled",
                Url = page.LinkTo("/jobs/scheduled"),
                Active = page.RequestPath.StartsWith("/jobs/scheduled"),
                Metric = new DashboardMetric(page.Statistics.Scheduled)
            });

            Items.Add(page => new DashboardMenuItem
            {
                Text = "Processing",
                Url = page.LinkTo("/jobs/processing"),
                Active = page.RequestPath.StartsWith("/jobs/processing"),
                Metric = new DashboardMetric(page.Statistics.Processing)
            });

            Items.Add(page => new DashboardMenuItem
            {
                Text = "Succeeded",
                Url = page.LinkTo("/jobs/succeeded"),
                Active = page.RequestPath.StartsWith("/jobs/succeeded"),
                Metric = new DashboardMetric(page.Statistics.Succeeded)
            });

            Items.Add(page => new DashboardMenuItem
            {
                Text = "Failed",
                Url = page.LinkTo("/jobs/failed"),
                Active = page.RequestPath.StartsWith("/jobs/failed"),
                Metric = new DashboardMetric(page.Statistics.Failed)
            });

            Items.Add(page => new DashboardMenuItem
            {
                Text = "Deleted",
                Url = page.LinkTo("/jobs/deleted"),
                Active = page.RequestPath.StartsWith("/jobs/deleted"),
                Metric = new DashboardMetric(page.Statistics.Deleted)
            });
        }
    }
}
