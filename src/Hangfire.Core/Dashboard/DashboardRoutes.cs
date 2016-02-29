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
using System.Reflection;
using Hangfire.Dashboard.Pages;
using Hangfire.States;

namespace Hangfire.Dashboard
{
    public static class DashboardRoutes
    {
        private static readonly string[] Javascripts =
        {
            "jquery-2.1.4.min.js", 
            "bootstrap.min.js",
            "moment.min.js",
            "moment-with-locales.min.js",
            "d3.min.js", 
            "d3.layout.min.js", 
            "rickshaw.min.js", 
            "hangfire.js"
        };

        private static readonly string[] Stylesheets =
        {
            "bootstrap.min.css", 
            "rickshaw.min.css", 
            "hangfire.css"
        };

        static DashboardRoutes()
        {
            Routes = new RouteCollection();
            Routes.AddRazorPage("/", x => new HomePage());
            Routes.Add("/stats", new JsonStats());
            
            #region Embedded static content

            Routes.Add("/js", new CombinedResourceDispatcher(
                "application/javascript",
                GetExecutingAssembly(),
                GetContentFolderNamespace("js"),
                Javascripts));

            Routes.Add("/css", new CombinedResourceDispatcher(
                "text/css",
                GetExecutingAssembly(),
                GetContentFolderNamespace("css"),
                Stylesheets));

            Routes.Add("/fonts/glyphicons-halflings-regular/eot", new EmbeddedResourceDispatcher(
                "application/vnd.ms-fontobject",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.eot")));

            Routes.Add("/fonts/glyphicons-halflings-regular/svg", new EmbeddedResourceDispatcher(
                "image/svg+xml",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.svg")));

            Routes.Add("/fonts/glyphicons-halflings-regular/ttf", new EmbeddedResourceDispatcher(
                "application/octet-stream",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.ttf")));

            Routes.Add("/fonts/glyphicons-halflings-regular/woff", new EmbeddedResourceDispatcher(
                "font/woff",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.woff")));

            Routes.Add("/fonts/glyphicons-halflings-regular/woff2", new EmbeddedResourceDispatcher(
                "font/woff2",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.woff2")));

            #endregion

            #region Razor pages and commands

            Routes.AddRazorPage("/jobs/enqueued", x => new QueuesPage());
            Routes.AddRazorPage(
                "/jobs/enqueued/fetched/(?<Queue>.+)",
                x => new FetchedJobsPage(x.Groups["Queue"].Value));

            Routes.AddClientBatchCommand("/jobs/enqueued/delete", (client, jobId) => client.Delete(jobId));
            Routes.AddClientBatchCommand("/jobs/enqueued/requeue", (client, jobId) => client.Requeue(jobId));

            Routes.AddRazorPage(
                "/jobs/enqueued/(?<Queue>.+)",
                x => new EnqueuedJobsPage(x.Groups["Queue"].Value));

            Routes.AddRazorPage("/jobs/processing", x => new ProcessingJobsPage());
            Routes.AddClientBatchCommand(
                "/jobs/processing/delete", 
                (client, jobId) => client.Delete(jobId, ProcessingState.StateName));

            Routes.AddClientBatchCommand(
                "/jobs/processing/requeue",
                (client, jobId) => client.Requeue(jobId, ProcessingState.StateName));

            Routes.AddRazorPage("/jobs/scheduled", x => new ScheduledJobsPage());

            Routes.AddClientBatchCommand(
                "/jobs/scheduled/enqueue", 
                (client, jobId) => client.Requeue(jobId, ScheduledState.StateName));

            Routes.AddClientBatchCommand(
                "/jobs/scheduled/delete",
                (client, jobId) => client.Delete(jobId, ScheduledState.StateName));

            Routes.AddRazorPage("/jobs/succeeded", x => new SucceededJobs());
            Routes.AddClientBatchCommand(
                "/jobs/succeeded/requeue",
                (client, jobId) => client.Requeue(jobId, SucceededState.StateName));

            Routes.AddRazorPage("/jobs/failed", x => new FailedJobsPage());

            Routes.AddClientBatchCommand(
                "/jobs/failed/requeue",
                (client, jobId) => client.Requeue(jobId, FailedState.StateName));

            Routes.AddClientBatchCommand(
                "/jobs/failed/delete",
                (client, jobId) => client.Delete(jobId, FailedState.StateName));

            Routes.AddRazorPage("/jobs/deleted", x => new DeletedJobsPage());

            Routes.AddClientBatchCommand(
                "/jobs/deleted/requeue",
                (client, jobId) => client.Requeue(jobId, DeletedState.StateName));

            Routes.AddRazorPage("/jobs/awaiting", x => new AwaitingJobsPage());
            Routes.AddClientBatchCommand("/jobs/awaiting/enqueue", (client, jobId) => client.ChangeState(
                jobId, new EnqueuedState(), AwaitingState.StateName));
            Routes.AddClientBatchCommand("/jobs/awaiting/delete", (client, jobId) => client.ChangeState(
                jobId, new DeletedState(), AwaitingState.StateName));

            Routes.AddCommand(
                "/jobs/actions/requeue/(?<JobId>.+)",
                context =>
                {
                    var client = new BackgroundJobClient(context.JobStorage);
                    return client.Requeue(context.UriMatch.Groups["JobId"].Value);
                });

            Routes.AddCommand(
                "/jobs/actions/delete/(?<JobId>.+)",
                context =>
                {
                    var client = new BackgroundJobClient(context.JobStorage);
                    return client.Delete(context.UriMatch.Groups["JobId"].Value);
                });

            Routes.AddRazorPage("/jobs/details/(?<JobId>.+)", x => new JobDetailsPage(x.Groups["JobId"].Value));

            Routes.AddRazorPage("/recurring", x => new RecurringJobsPage());
            Routes.AddRecurringBatchCommand(
                "/recurring/remove", 
                (manager, jobId) => manager.RemoveIfExists(jobId));

            Routes.AddRecurringBatchCommand(
                "/recurring/trigger", 
                (manager, jobId) => manager.Trigger(jobId));

            Routes.AddRazorPage("/servers", x => new ServersPage());
            Routes.AddRazorPage("/retries", x => new RetriesPage());

            #endregion
        }

        public static RouteCollection Routes { get; private set; }

        internal static string GetContentFolderNamespace(string contentFolder)
        {
            return String.Format("{0}.Content.{1}", typeof(DashboardRoutes).Namespace, contentFolder);
        }

        internal static string GetContentResourceName(string contentFolder, string resourceName)
        {
            return String.Format("{0}.{1}", GetContentFolderNamespace(contentFolder), resourceName);
        }

        private static Assembly GetExecutingAssembly()
        {
            return typeof (DashboardRoutes).GetTypeInfo().Assembly;
        }
    }
}
