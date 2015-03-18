// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Linq.Expressions;
using System.Text;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Continuations;
using Hangfire.Dashboard;
using Hangfire.States;
using Hangfire.Storage;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Hangfire
{
    public static class ContinuationExtensions
    {
        public static IGlobalConfiguration UseContinuations([NotNull] this IGlobalConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            GlobalJobFilters.Filters.Add(new ContinuationsSupportAttribute());
            GlobalStateHandlers.Handlers.Add(new AwaitingState.Handler());

            DashboardRoutes.Routes.AddRazorPage("/jobs/awaiting", x => new AwaitingJobsPage());
            DashboardRoutes.Routes.AddClientBatchCommand("/jobs/awaiting/enqueue", (client, jobId) => client.ChangeState(
                jobId, new EnqueuedState(), AwaitingState.StateName));
            DashboardRoutes.Routes.AddClientBatchCommand("/jobs/awaiting/delete", (client, jobId) => client.ChangeState(
                jobId, new DeletedState(), AwaitingState.StateName));

            JobHistoryRenderer.AddBackgroundStateColor(AwaitingState.StateName, "#F5F5F5");
            JobHistoryRenderer.AddForegroundStateColor(AwaitingState.StateName, "#999");
            JobHistoryRenderer.Register(AwaitingState.StateName, (html, data) =>
            {
                var builder = new StringBuilder();

                builder.Append("<dl class=\"dl-horizontal\">");

                if (data.ContainsKey("ParentId"))
                {
                    builder.AppendFormat(
                        "<dt>Parent</dt><dd>{0}</dd>",
                        html.JobIdLink(data["ParentId"]));
                }

                if (data.ContainsKey("NextState"))
                {
                    var nextState = JsonConvert.DeserializeObject<IState>(
                        data["NextState"],
                        new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                    builder.AppendFormat(
                        "<dt>Next State</dt><dd>{0}</dd>",
                        html.StateLabel(nextState.Name));
                }

                if (data.ContainsKey("Options"))
                {
                    builder.AppendFormat(
                        "<dt>Options</dt><dd><code>{0}</code></dd>",
                        html.HtmlEncode(data["Options"]));
                }

                builder.Append("</dl>");

                return new NonEscapedString(builder.ToString());
            });

            DashboardMetrics.AddMetric(AwaitingCount);

            JobsSidebarMenu.Items.Add(page => new MenuItem("Awaiting", page.Url.To("/jobs/awaiting"))
            {
                Active = page.RequestPath.StartsWith("/jobs/awaiting"),
                Metric = AwaitingCount
            });

            return configuration;
        }

        public static readonly DashboardMetric AwaitingCount = new DashboardMetric(
            "awaiting:count",
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

                return new Metric(awaitingCount.ToString("N0"))
                {
                    Style = awaitingCount > 0 ? MetricStyle.Info : MetricStyle.Default
                };
            });

        public static string ContinueWith(this IBackgroundJobClient client, string parentId,
            [InstantHandle] Expression<Action> methodCall)
        {
            return ContinueWith(client, parentId, methodCall, new EnqueuedState());
        }

        public static string ContinueWith(
            this IBackgroundJobClient client,
            string parentId,
            [InstantHandle] Expression<Action> methodCall,
            IState nextState)
        {
            return ContinueWith(client, parentId, methodCall, nextState, JobContinuationOptions.OnlyOnSucceededState);
        }

        public static string ContinueWith(
            [NotNull] this IBackgroundJobClient client,
            string parentId,
            [InstantHandle] Expression<Action> methodCall,
            JobContinuationOptions options)
        {
            return ContinueWith(client, parentId, methodCall, new EnqueuedState(), options);
        }

        public static string ContinueWith(
            [NotNull] this IBackgroundJobClient client,
            string parentId,
            [InstantHandle] Expression<Action> methodCall,
            IState nextState,
            JobContinuationOptions options)
        {
            if (client == null) throw new ArgumentNullException("client");

            var state = new AwaitingState(parentId, nextState, options);
            return client.Create(Job.FromExpression(methodCall), state);
        }
    }
}
