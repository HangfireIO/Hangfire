﻿// This file is part of Hangfire. Copyright © 2015 Hangfire OÜ.
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
using System.Globalization;
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
            static page => new Metric(page.Statistics.Servers)
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
            static page => new Metric(page.Statistics.Recurring));

        public static readonly DashboardMetric RetriesCount = new DashboardMetric(
            "retries:count",
            "Metrics_Retries",
            static page =>
            {
                long retryCount;

                if (page.Statistics.Retries.HasValue)
                {
                    retryCount = page.Statistics.Retries.Value;
                }
                else
                {
                    using (var connection = page.Storage.GetReadOnlyConnection())
                    {
                        if (!(connection is JobStorageConnection storageConnection))
                        {
                            return null;
                        }

                        retryCount = storageConnection.GetSetCount("retries");
                    }
                }

                return new Metric(retryCount)
                {
                    Style = retryCount > 0 ? MetricStyle.Warning : MetricStyle.Default
                };
            });

        public static readonly DashboardMetric EnqueuedCountOrNull = new DashboardMetric(
            "enqueued:count-or-null",
            "Metrics_EnqueuedCountOrNull",
            static page => page.Statistics.Enqueued > 0 || page.Statistics.Failed == 0
                ? new Metric(page.Statistics.Enqueued > 0 ? page.Statistics.Enqueued : page.Statistics.Scheduled)
                {
                    Style = page.Statistics.Enqueued + page.Statistics.Scheduled > 0 ? MetricStyle.Info : MetricStyle.Default,
                    Highlighted = page.Statistics.Enqueued > 0 && page.Statistics.Failed == 0
                }
                : null);

        public static readonly DashboardMetric FailedCountOrNull = new DashboardMetric(
            "failed:count-or-null",
            "Metrics_FailedJobs",
            static page => page.Statistics.Failed > 0
                ? new Metric(page.Statistics.Failed)
                {
                    Style = MetricStyle.Danger,
                    Highlighted = true,
                    Title = string.Format(CultureInfo.CurrentCulture, Strings.Metrics_FailedCountOrNull, page.Statistics.Failed)
                }
                : null);

        public static readonly DashboardMetric EnqueuedAndQueueCount = new DashboardMetric(
            "enqueued-queues:count",
            "Metrics_EnqueuedQueuesCount",
            static page => new Metric($"{page.Statistics.Enqueued:N0} / {page.Statistics.Queues:N0}")
            {
                IntValue = page.Statistics.Enqueued,
                Style = page.Statistics.Enqueued > 0 ? MetricStyle.Info : MetricStyle.Default,
                Highlighted = page.Statistics.Enqueued > 0
            });

        public static readonly DashboardMetric ScheduledCount = new DashboardMetric(
            "scheduled:count",
            "Metrics_ScheduledJobs",
            static page => new Metric(page.Statistics.Scheduled)
            {
                Style = page.Statistics.Scheduled > 0 ? MetricStyle.Info : MetricStyle.Default
            });

        public static readonly DashboardMetric ProcessingCount = new DashboardMetric(
            "processing:count",
            "Metrics_ProcessingJobs",
            static page => new Metric(page.Statistics.Processing)
            {
                Style = page.Statistics.Processing > 0 ? MetricStyle.Warning : MetricStyle.Default
            });

        public static readonly DashboardMetric SucceededCount = new DashboardMetric(
            "succeeded:count",
            "Metrics_SucceededJobs",
            static page => new Metric(page.Statistics.Succeeded));

        public static readonly DashboardMetric FailedCount = new DashboardMetric(
            "failed:count",
            "Metrics_FailedJobs",
            static page => new Metric(page.Statistics.Failed)
            {
                Style = page.Statistics.Failed > 0 ? MetricStyle.Danger : MetricStyle.Default,
                Highlighted = page.Statistics.Failed > 0
            });

        public static readonly DashboardMetric DeletedCount = new DashboardMetric(
            "deleted:count",
            "Metrics_DeletedJobs",
            static page => new Metric(page.Statistics.Deleted));

        public static readonly DashboardMetric AwaitingCount = new DashboardMetric(
            "awaiting:count",
            "Metrics_AwaitingCount",
            static page =>
            {
                long awaitingCount = -1;

                if (page.Statistics.Awaiting.HasValue)
                {
                    awaitingCount = page.Statistics.Awaiting.Value;
                }
                else
                {
                    using (var connection = page.Storage.GetReadOnlyConnection())
                    {
                        if (connection is JobStorageConnection storageConnection)
                        {
                            awaitingCount = storageConnection.GetSetCount("awaiting");
                        }
                    }
                }

                return new Metric(awaitingCount)
                {
                    Style = awaitingCount > 0 ? MetricStyle.Info : MetricStyle.Default
                };
            });
    }
}
