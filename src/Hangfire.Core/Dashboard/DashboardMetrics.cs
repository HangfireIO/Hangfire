using System;
using System.Collections.Generic;
using Hangfire.Storage;

namespace Hangfire.Dashboard
{
    public class DashboardMetric
    {
        public DashboardMetric(string name, Func<RazorPage, Metric> func)
            : this(name, name, func)
        {
        }

        public DashboardMetric(string name, string title, Func<RazorPage, Metric> func)
        {
            Name = name;
            Title = title;
            Func = func;
        }

        public string Name { get; private set; }
        public Func<RazorPage, Metric> Func { get; private set; }

        public string Title { get; set; }
    }

    public class DashboardMetrics
    {
        public static readonly DashboardMetric ServerCount = new DashboardMetric(
            "servers:count", 
            "Servers",
            page => new Metric(page.Statistics.Servers.ToString("N0"))
            {
                Style = page.Statistics.Servers == 0 ? MetricStyle.Warning : MetricStyle.Default,
                Highlighted = page.Statistics.Servers == 0,
                Title = page.Statistics.Servers == 0
                    ? "No active servers found. Jobs will not be processed."
                    : null
            });

        public static readonly DashboardMetric RecurringJobCount = new DashboardMetric(
            "recurring:count",
            "Recurring Jobs",
            page => new Metric(page.Statistics.Recurring.ToString("N0")));

        public static readonly DashboardMetric RetriesCount = new DashboardMetric(
            "retries:count",
            "Retries",
            page =>
            {
                long retryCount;
                using (var connection = page.Storage.GetConnection())
                {
                    var storageConnection = connection as JobStorageConnection;
                    if (storageConnection == null)
                    {
                        return null;
                    }

                    retryCount = storageConnection.GetSetCount("retries");
                }

                return new Metric(retryCount.ToString("N0"))
                {
                    Style = retryCount > 0 ? MetricStyle.Warning : MetricStyle.Default
                };
            });

        public static readonly DashboardMetric EnqueuedCountOrNull = new DashboardMetric(
            "enqueued:count-or-null",
            page => page.Statistics.Enqueued > 0 || page.Statistics.Failed == 0
                ? new Metric(page.Statistics.Enqueued.ToString("N0"))
                {
                    Style = page.Statistics.Enqueued > 0 ? MetricStyle.Info : MetricStyle.Default,
                    Highlighted = page.Statistics.Enqueued > 0 && page.Statistics.Failed == 0
                }
                : null);

        public static readonly DashboardMetric FailedCountOrNull = new DashboardMetric(
            "failed:count-or-null",
            page => page.Statistics.Failed > 0
                ? new Metric(page.Statistics.Failed.ToString("N0"))
                {
                    Style = MetricStyle.Danger,
                    Highlighted = true,
                    Title =
                        String.Format("{0} failed job(s) found. Retry or delete them manually.", page.Statistics.Failed)
                }
                : null);

        public static readonly DashboardMetric EnqueuedAndQueueCount = new DashboardMetric(
            "enqueued-queues:count",
            page => new Metric(String.Format("{0:N0} / {1:N0}", page.Statistics.Enqueued, page.Statistics.Queues))
            {
                Style = page.Statistics.Enqueued > 0 ? MetricStyle.Info : MetricStyle.Default,
                Highlighted = page.Statistics.Enqueued > 0
            });

        public static readonly DashboardMetric ScheduledCount = new DashboardMetric(
            "scheduled:count",
            "Scheduled Jobs",
            page => new Metric(page.Statistics.Scheduled.ToString("N0"))
            {
                Style = page.Statistics.Scheduled > 0 ? MetricStyle.Info : MetricStyle.Default
            });

        public static readonly DashboardMetric ProcessingCount = new DashboardMetric(
            "processing:count",
            "Processing Jobs",
            page => new Metric(page.Statistics.Processing.ToString("N0"))
            {
                Style = page.Statistics.Processing > 0 ? MetricStyle.Warning : MetricStyle.Default
            });

        public static readonly DashboardMetric SucceededCount = new DashboardMetric(
            "succeeded:count",
            "Succeeded Jobs",
            page => new Metric(page.Statistics.Succeeded.ToString("N0"))
            {
                IntValue = page.Statistics.Succeeded
            });

        public static readonly DashboardMetric FailedCount = new DashboardMetric(
            "failed:count",
            "Failed Jobs",
            page => new Metric(page.Statistics.Failed.ToString("N0"))
            {
                IntValue = page.Statistics.Failed,
                Style = page.Statistics.Failed > 0 ? MetricStyle.Danger : MetricStyle.Default,
                Highlighted = page.Statistics.Failed > 0
            });

        public static readonly DashboardMetric DeletedCount = new DashboardMetric(
            "deleted:count",
            "Deleted Jobs",
            page => new Metric(page.Statistics.Deleted.ToString("N0")));

        static DashboardMetrics()
        {
            Add(ServerCount);
            Add(RecurringJobCount);
            Add(RetriesCount);
            Add(EnqueuedCountOrNull);
            Add(FailedCountOrNull);
            Add(EnqueuedAndQueueCount);
            Add(ScheduledCount);
            Add(ProcessingCount);
            Add(SucceededCount);
            Add(FailedCount);
            Add(DeletedCount);
        }

        public static List<DashboardMetric> Metrics = new List<DashboardMetric>(); 

        public static void Add(DashboardMetric metric)
        {
            Metrics.Add(metric);
        }
    }
}
