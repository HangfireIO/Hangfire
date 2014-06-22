// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using HangFire.Dashboard.Pages;
using HangFire.States;

namespace HangFire.Dashboard
{
    public static class DashboardRoutes
    {
        private static readonly string[] Javascripts =
        {
            "jquery-1.10.2.min.js", 
            "bootstrap.min.js",
            "moment.min.js",
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
            Routes.AddRazorPage("/", x => new DashboardPage());
            Routes.Add("/stats", new JsonStats());
            
            #region Embedded static content

            Routes.Add("/js", new CombinedResourceDispatcher(
                "application/javascript", 
                typeof(DashboardRoutes).Assembly,
                GetContentFolderNamespace("js"),
                Javascripts));

            Routes.Add("/css", new CombinedResourceDispatcher(
                "text/css",
                typeof(DashboardRoutes).Assembly,
                GetContentFolderNamespace("css"),
                Stylesheets));

            Routes.Add("/fonts/glyphicons-halflings-regular/eot", new EmbeddedResourceDispatcher(
                "application/vnd.ms-fontobject",
                typeof(DashboardRoutes).Assembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.eot")));

            Routes.Add("/fonts/glyphicons-halflings-regular/svg", new EmbeddedResourceDispatcher(
                "image/svg+xml",
                typeof(DashboardRoutes).Assembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.svg")));

            Routes.Add("/fonts/glyphicons-halflings-regular/ttf", new EmbeddedResourceDispatcher(
                "application/octet-stream",
                typeof(DashboardRoutes).Assembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.ttf")));

            Routes.Add("/fonts/glyphicons-halflings-regular/woff", new EmbeddedResourceDispatcher(
                "application/font-woff",
                typeof(DashboardRoutes).Assembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.woff")));

            #endregion

            #region Razor pages and commands

            Routes.AddRazorPage("/job/(?<JobId>.+)", x => new JobDetailsPage(x.Groups["JobId"].Value));

            Routes.AddRazorPage("/queues", x => new QueuesPage());
            Routes.AddRazorPage(
                "/queues/fetched/(?<Queue>.+)",
                x => new FetchedJobsPage(x.Groups["Queue"].Value));
            Routes.AddRazorPage(
                "/queues/(?<Queue>.+)",
                x => new EnqueuedJobsPage(x.Groups["Queue"].Value));

            Routes.AddBatchCommand("/enqueued/delete", jobId => BackgroundJob.Delete(jobId));
            Routes.AddBatchCommand("/enqueued/requeue", jobId => BackgroundJob.Requeue(jobId));

            Routes.AddRazorPage("/processing", x => new ProcessingJobsPage());
            Routes.AddBatchCommand(
                "/processing/delete", 
                jobId => BackgroundJob.Delete(jobId, ProcessingState.StateName));

            Routes.AddBatchCommand(
                "/processing/requeue",
                jobId => BackgroundJob.Requeue(jobId, ProcessingState.StateName));

            Routes.AddRazorPage("/scheduled", x => new ScheduledJobsPage());

            Routes.AddBatchCommand(
                "/scheduled/enqueue", 
                jobId => BackgroundJob.Requeue(jobId, ScheduledState.StateName));

            Routes.AddBatchCommand(
                "/scheduled/delete",
                jobId => BackgroundJob.Delete(jobId, ScheduledState.StateName));

            Routes.AddRazorPage("/servers", x => new ServersPage());
            Routes.AddRazorPage("/succeeded", x => new SucceededJobs());
            Routes.AddBatchCommand(
                "/succeeded/requeue",
                jobId => BackgroundJob.Requeue(jobId, SucceededState.StateName));

            Routes.AddRazorPage("/failed", x => new FailedJobsPage());

            Routes.AddBatchCommand(
                "/failed/requeue",
                jobId => BackgroundJob.Requeue(jobId, FailedState.StateName));

            Routes.AddBatchCommand(
                "/failed/delete",
                jobId => BackgroundJob.Delete(jobId, FailedState.StateName));

            Routes.AddRazorPage("/deleted", x => new DeletedJobsPage());

            Routes.AddBatchCommand(
                "/deleted/requeue",
                jobId => BackgroundJob.Requeue(jobId, DeletedState.StateName));

            Routes.AddCommand(
                "/actions/requeue/(?<JobId>.+)",
                x => BackgroundJob.Requeue(x.Groups["JobId"].Value));

            Routes.AddCommand(
                "/actions/delete/(?<JobId>.+)",
                x => BackgroundJob.Delete(x.Groups["JobId"].Value));

            Routes.AddRazorPage("/recurring", x => new RecurringJobsPage());
            Routes.AddBatchCommand("/recurring/remove", RecurringJob.RemoveIfExists);
            Routes.AddBatchCommand("/recurring/trigger", RecurringJob.Trigger);

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
    }
}
