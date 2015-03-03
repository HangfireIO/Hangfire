using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hangfire.Annotations;
using Hangfire.Storage.Monitoring;

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

    public class RazorPageContext
    {
        public RazorPageContext(RazorPage page, StatisticsDto statistics)
        {
            Page = page;
            Statistics = statistics;
        }

        public RazorPage Page { get; private set; }
        public StatisticsDto Statistics { get; private set; }
    }

    public static class DashboardMenu
    {
        private static readonly List<Func<RazorPageContext, DashboardMenuItem>> Items
            = new List<Func<RazorPageContext, DashboardMenuItem>>();

        static DashboardMenu()
        {
            Items.Add(context => new DashboardMenuItem
            {
                Text = "Jobs",
                Url = context.Page.LinkTo("/jobs/enqueued"),
                Active = context.Page.RequestPath.StartsWith("/jobs"),
                Metric = context.Statistics.Failed != 0 
                    ? new DashboardMetric(context.Statistics.Failed)
                    {
                        Style = MetricStyle.Danger,
                        Highlighted = true
                    }
                    : new DashboardMetric(context.Statistics.Enqueued)
                    {
                        Style = MetricStyle.Success
                    }
            });

            Items.Add(context => new DashboardMenuItem
            {
                Text = "Retries",
                Url = "#",
                Active = false,
                Metric = new DashboardMetric(12)
                {
                    Style = MetricStyle.Warning
                }
            });

            Items.Add(context => new DashboardMenuItem
            {
                Text = "Recurring",
                Url = context.Page.LinkTo("/recurring"),
                Active = context.Page.RequestPath.StartsWith("/recurring"),
                Metric = new DashboardMetric(context.Statistics.Recurring)
            });

            Items.Add(context => new DashboardMenuItem
            {
                Text = "Servers",
                Url = context.Page.LinkTo("/servers"),
                Active = context.Page.RequestPath.Equals("/servers"),
                Metric = new DashboardMetric(context.Statistics.Servers)
                {
                    Style = context.Statistics.Servers == 0 ? MetricStyle.Warning : MetricStyle.None,
                    Highlighted = context.Statistics.Servers == 0
                }
            });
        }

        public static NonEscapedString Render(RazorPage page)
        {
            var monitoringApi = page.Storage.GetMonitoringApi();
            var statistics = monitoringApi.GetStatistics();

            var context = new RazorPageContext(page, statistics);
            var builder = new StringBuilder();

            if (Items.Count == 0) return null;

            builder.Append("<ul class=\"nav navbar-nav\">");

            foreach (var itemsFunc in Items)
            {
                var item = itemsFunc(context);

                builder.AppendFormat("<li{0}>", item.Active ? " class=\"active\"" : null);
                builder.AppendFormat("<a href=\"{0}\">", item.Url);
                // TODO: Escape string
                builder.Append(item.Text);

                if (item.Metric != null)
                {
                    string className;
                    switch (item.Metric.Style)
                    {
                        case MetricStyle.Info:
                            className = "metric-info";
                            break;
                        case MetricStyle.Success:
                            className = "metric-success";
                            break;
                        case MetricStyle.Warning:
                            className = "metric-warning";
                            break;
                        case MetricStyle.Danger:
                            className = "metric-danger";
                            break;
                        default:
                            className = "metric-default";
                            break;
                    }

                    builder.AppendFormat(
                        " <span class=\"metric {0}{1}\">{2}</span>",
                        className,
                        item.Metric.Highlighted ? " highlighted" : null,
                        String.Join("&nbsp;/&nbsp;", item.Metric.Values));
                }

                builder.Append("</a></li>");
            }

            builder.Append("</ul>");
            return new NonEscapedString(builder.ToString());
        }
    }

    public static class SidebarMenu
    {
        private static readonly List<Func<RazorPageContext, DashboardMenuItem>> Items
            = new List<Func<RazorPageContext, DashboardMenuItem>>();

        static SidebarMenu()
        {
            Items.Add(context => new DashboardMenuItem
            {
                Text = "Enqueued",
                Url = context.Page.LinkTo("/jobs/enqueued"),
                Active = context.Page.RequestPath.StartsWith("/jobs/enqueued"),
                Metric = new DashboardMetric(context.Statistics.Enqueued, context.Statistics.Queues)
            });

            Items.Add(context => new DashboardMenuItem
            {
                Text = "Scheduled",
                Url = context.Page.LinkTo("/jobs/scheduled"),
                Active = context.Page.RequestPath.StartsWith("/jobs/scheduled"),
                Metric = new DashboardMetric(context.Statistics.Scheduled)
            });

            Items.Add(context => new DashboardMenuItem
            {
                Text = "Processing",
                Url = context.Page.LinkTo("/jobs/processing"),
                Active = context.Page.RequestPath.StartsWith("/jobs/processing"),
                Metric = new DashboardMetric(context.Statistics.Processing)
            });

            Items.Add(context => new DashboardMenuItem
            {
                Text = "Succeeded",
                Url = context.Page.LinkTo("/jobs/succeeded"),
                Active = context.Page.RequestPath.StartsWith("/jobs/succeeded"),
                Metric = new DashboardMetric(context.Statistics.Succeeded)
            });

            Items.Add(context => new DashboardMenuItem
            {
                Text = "Failed",
                Url = context.Page.LinkTo("/jobs/failed"),
                Active = context.Page.RequestPath.StartsWith("/jobs/failed"),
                Metric = new DashboardMetric(context.Statistics.Failed)
            });

            Items.Add(context => new DashboardMenuItem
            {
                Text = "Deleted",
                Url = context.Page.LinkTo("/jobs/deleted"),
                Active = context.Page.RequestPath.StartsWith("/jobs/deleted"),
                Metric = new DashboardMetric(context.Statistics.Deleted)
            });
        }

        internal static NonEscapedString Render([NotNull] RazorPage page)
        {
            if (page == null) throw new ArgumentNullException("page");

            var builder = new StringBuilder();

            var monitoringApi = page.Storage.GetMonitoringApi();
            var statistics = monitoringApi.GetStatistics();
            var context = new RazorPageContext(page, statistics);

            builder.Append("<div id=\"stats\" class=\"list-group\">");

            foreach (var itemsFunc in Items)
            {
                var item = itemsFunc(context);

                builder.AppendFormat("<a href=\"{0}\" class=\"list-group-item{1}\">", item.Url, item.Active ? " active" : null);
                // TODO: Escape string
                builder.Append(item.Text);

                if (item.Metric != null)
                {
                    string className;
                    switch (item.Metric.Style)
                    {
                        case MetricStyle.Info:
                            className = "metric-info";
                            break;
                        case MetricStyle.Success:
                            className = "metric-success";
                            break;
                        case MetricStyle.Warning:
                            className = "metric-warning";
                            break;
                        case MetricStyle.Danger:
                            className = "metric-danger";
                            break;
                        default:
                            className = "metric-default";
                            break;
                    }

                    builder.AppendFormat(
                        " <span class=\"pull-right metric {0}{1}\">{2}</span>",
                        className,
                        item.Metric.Highlighted ? " highlighted" : null,
                        String.Join("&nbsp;/&nbsp;", item.Metric.Values));
                }

                builder.Append("</a>");
            }

            builder.Append("</div>");

            return new NonEscapedString(builder.ToString());
        }
    }
}
