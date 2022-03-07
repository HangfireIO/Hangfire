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
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Dashboard.Resources;
using Hangfire.Storage;

namespace Hangfire.Dashboard
{
    public static class DashboardMetrics
    {
        private static readonly Dictionary<string, DashboardMetric> Metrics = new Dictionary<string, DashboardMetric>();

        static DashboardMetrics()
        {
            AddMetric(ServerCount);
            AddMetric(RecurringJobCount);
            AddMetric(RetriesCount);
            AddMetric(EnqueuedCountOrNull);
            AddMetric(FailedCountOrNull);
            AddMetric(EnqueuedAndQueueCount);
            AddMetric(ScheduledCount);
            AddMetric(ProcessingCount);
            AddMetric(SucceededCount);
            AddMetric(FailedCount);
            AddMetric(DeletedCount);
            AddMetric(AwaitingCount);
        }

        public static void AddMetric([NotNull] DashboardMetric metric)
        {
            if (metric == null) throw new ArgumentNullException(nameof(metric));

            lock (Metrics)
            {
                Metrics[metric.Name] = metric;
            }
        }

        public static IEnumerable<DashboardMetric> GetMetrics()
        {
            lock (Metrics)
            {
                return Metrics.Values.ToList();
            }
        }

        public static readonly DashboardMetric ServerCount = new DashboardMetric(
            "servers:count", 
            "Metrics_Servers",
            page => new Metric(page.Statistics.Servers)
            {
                Style = page.Statistics.Servers == 0 ? MetricStyle.Warning : MetricStyle.Default,
                Highlighted = page.Statistics.Servers == 0,
                Title = page.Statistics.Servers == 0
                    ? "No active servers found. Jobs will not be processed."
                    : null
            });

        public static readonly DashboardMetric RecurringJobCount = new DashboardMetric(
            "recurring:count",
            "Metrics_RecurringJobs",
            page => new Metric(page.Statistics.Recurring));

        public static readonly DashboardMetric RetriesCount = new DashboardMetric(
            "retries:count",
            "Metrics_Retries",
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

                return new Metric(retryCount)
                {
                    Style = retryCount > 0 ? MetricStyle.Warning : MetricStyle.Default
                };
            });

        public static readonly DashboardMetric EnqueuedCountOrNull = new DashboardMetric(
            "enqueued:count-or-null",
            "Metrics_EnqueuedCountOrNull",
            page => page.Statistics.Enqueued > 0 || page.Statistics.Failed == 0
                ? new Metric(page.Statistics.Enqueued)
                {
                    Style = page.Statistics.Enqueued > 0 ? MetricStyle.Info : MetricStyle.Default,
                    Highlighted = page.Statistics.Enqueued > 0 && page.Statistics.Failed == 0
                }
                : null);

        public static readonly DashboardMetric FailedCountOrNull = new DashboardMetric(
            "failed:count-or-null",
            "Metrics_FailedJobs",
            page => page.Statistics.Failed > 0
                ? new Metric(page.Statistics.Failed)
                {
                    Style = MetricStyle.Danger,
                    Highlighted = true,
                    Title = string.Format(Strings.Metrics_FailedCountOrNull, page.Statistics.Failed)
                }
                : null);

        public static readonly DashboardMetric EnqueuedAndQueueCount = new DashboardMetric(
            "enqueued-queues:count",
            "Metrics_EnqueuedQueuesCount",
            page => new Metric($"{page.Statistics.Enqueued:N0} / {page.Statistics.Queues:N0}")
            {
                IntValue = page.Statistics.Enqueued,
                Style = page.Statistics.Enqueued > 0 ? MetricStyle.Info : MetricStyle.Default,
                Highlighted = page.Statistics.Enqueued > 0
            });

        public static readonly DashboardMetric ScheduledCount = new DashboardMetric(
            "scheduled:count",
            "Metrics_ScheduledJobs",
            page => new Metric(page.Statistics.Scheduled)
            {
                Style = page.Statistics.Scheduled > 0 ? MetricStyle.Info : MetricStyle.Default
            });

        public static readonly DashboardMetric ProcessingCount = new DashboardMetric(
            "processing:count",
            "Metrics_ProcessingJobs",
            page => new Metric(page.Statistics.Processing)
            {
                Style = page.Statistics.Processing > 0 ? MetricStyle.Warning : MetricStyle.Default
            });

        public static readonly DashboardMetric SucceededCount = new DashboardMetric(
            "succeeded:count",
            "Metrics_SucceededJobs",
            page => new Metric(page.Statistics.Succeeded));

        public static readonly DashboardMetric FailedCount = new DashboardMetric(
            "failed:count",
            "Metrics_FailedJobs",
            page => new Metric(page.Statistics.Failed)
            {
                Style = page.Statistics.Failed > 0 ? MetricStyle.Danger : MetricStyle.Default,
                Highlighted = page.Statistics.Failed > 0
            });

        public static readonly DashboardMetric DeletedCount = new DashboardMetric(
            "deleted:count",
            "Metrics_DeletedJobs",
            page => new Metric(page.Statistics.Deleted));

        public static readonly DashboardMetric AwaitingCount = new DashboardMetric(
            "awaiting:count",
            "Metrics_AwaitingCount",
            page =>
            {
                long awaitingCount = -1;

                using (var connection = page.Storage.GetConnection())
                {
                    var storageConnection = connection as JobStorageConnection;
                    if (storageConnection != null)
                    {
                        awaitingCount = storageConnection.GetSetCount("awaiting");
                    }
                }

                return new Metric(awaitingCount)
                {
                    Style = awaitingCount > 0 ? MetricStyle.Info : MetricStyle.Default
                };
            });
    }
}
