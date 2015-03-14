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
using System.Linq;
using Hangfire.Annotations;
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
        }

        public static void AddMetric([NotNull] DashboardMetric metric)
        {
            if (metric == null) throw new ArgumentNullException("metric");

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
    }
}
