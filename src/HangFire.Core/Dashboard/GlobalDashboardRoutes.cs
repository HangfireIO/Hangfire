using System;
using HangFire.Dashboard.Pages;
using HangFire.States;

namespace HangFire.Dashboard
{
    public static class GlobalDashboardRoutes
    {
        private static readonly string[] Javascripts =
        {
            "jquery-1.10.2.min.js", 
            "bootstrap.min.js",
            "moment.min.js",
            "d3.min.js", 
            "d3.layout.min.js", 
            "rickshaw.min.js", 
            "hangfire.js",
        };

        private static readonly string[] Stylesheets =
        {
            "bootstrap.min.css", 
            "rickshaw.min.css", 
            "hangfire.css",
        };

        static GlobalDashboardRoutes()
        {
            Routes = new DashboardRouteCollection();
            Routes.AddRazorPage("/", x => new DashboardPage());
            Routes.Add("/stats", new JsonStats());
            
            #region Embedded static content

            Routes.Add("/js", new CombinedResourceDispatcher(
                "application/javascript", 
                typeof(GlobalDashboardRoutes).Assembly,
                GetContentFolderNamespace("js"),
                Javascripts));

            Routes.Add("/css", new CombinedResourceDispatcher(
                "text/css",
                typeof(GlobalDashboardRoutes).Assembly,
                GetContentFolderNamespace("css"),
                Stylesheets));

            Routes.Add("/fonts/glyphicons-halflings-regular.eot", new EmbeddedResourceDispatcher(
                "application/vnd.ms-fontobject",
                typeof(GlobalDashboardRoutes).Assembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.eot")));

            Routes.Add("/fonts/glyphicons-halflings-regular.svg", new EmbeddedResourceDispatcher(
                "image/svg+xml",
                typeof(GlobalDashboardRoutes).Assembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.svg")));

            Routes.Add("/fonts/glyphicons-halflings-regular.ttf", new EmbeddedResourceDispatcher(
                "application/octet-stream",
                typeof(GlobalDashboardRoutes).Assembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.ttf")));

            Routes.Add("/fonts/glyphicons-halflings-regular.woff", new EmbeddedResourceDispatcher(
                "application/font-woff",
                typeof(GlobalDashboardRoutes).Assembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.woff")));

            #endregion

            #region Razor pages and actions

            Routes.AddRazorPage("/job/(?<JobId>.+)", x => new JobDetailsPage(x.Groups["JobId"].Value));

            Routes.AddRazorPage("/queues", x => new QueuesPage());
            Routes.AddRazorPage(
                "/queues/fetched/(?<Queue>.+)",
                x => new FetchedJobsPage(x.Groups["Queue"].Value));
            Routes.AddRazorPage(
                "/queues/(?<Queue>.+)",
                x => new EnqueuedJobsPage(x.Groups["Queue"].Value));

            /*Routes.AddRazorPage("/enqueued/delete", x => new BatchCommandHandler(
                jobId => BackgroundJob.Delete(jobId)));

            Routes.AddRazorPage("/enqueued/requeue", x => new BatchCommandHandler(
                jobId => BackgroundJob.Requeue(jobId)));*/

            Routes.AddRazorPage("/processing", x => new ProcessingJobsPage());
            /*Routes.AddRazorPage("/processing/delete", x => new BatchCommandHandler(
                jobId => BackgroundJob.Delete(jobId, ProcessingState.StateName)));

            Routes.AddRazorPage("/processing/requeue", x => new BatchCommandHandler(
                jobId => BackgroundJob.Requeue(jobId, ProcessingState.StateName)));*/

            Routes.AddRazorPage("/scheduled", x => new ScheduledJobsPage());

            /*Routes.AddRazorPage("/scheduled/enqueue", x => new BatchCommandHandler(
                jobId => BackgroundJob.Requeue(jobId, ScheduledState.StateName)));

            Routes.AddRazorPage("/scheduled/delete", x => new BatchCommandHandler(
                jobId => BackgroundJob.Delete(jobId, ScheduledState.StateName)));*/

            Routes.AddRazorPage("/servers", x => new ServersPage());
            Routes.AddRazorPage("/succeeded", x => new SucceededJobs());
            /*Routes.AddRazorPage("/succeeded/requeue", x => new BatchCommandHandler(
                jobId => BackgroundJob.Requeue(jobId, SucceededState.StateName)));*/

            Routes.AddRazorPage("/failed", x => new FailedJobsPage());

            /*Routes.AddRazorPage("/failed/requeue", x => new BatchCommandHandler(
                jobId => BackgroundJob.Requeue(jobId, FailedState.StateName)));

            Routes.AddRazorPage("/failed/delete", x => new BatchCommandHandler(
                jobId => BackgroundJob.Delete(jobId, FailedState.StateName)));

            Routes.AddRazorPage("/deleted", x => new DeletedJobsPage());

            Routes.AddRazorPage("/deleted/requeue", x => new BatchCommandHandler(
                jobId => BackgroundJob.Requeue(jobId, DeletedState.StateName)));*/

            Routes.AddCommand(
                "/actions/requeue/(?<JobId>.+)",
                x => BackgroundJob.Requeue(x.Groups["JobId"].Value));

            Routes.AddCommand(
                "/actions/delete/(?<JobId>.+)",
                x => BackgroundJob.Delete(x.Groups["JobId"].Value));

            Routes.AddRazorPage("/recurring", x => new RecurringJobsPage());
            /*Routes.AddRazorPage("/recurring/remove", x => new BatchCommandHandler(
                RecurringJob.RemoveIfExists));
            Routes.AddRazorPage("/recurring/trigger", x => new BatchCommandHandler(
                RecurringJob.Trigger));*/

            #endregion
        }

        public static DashboardRouteCollection Routes { get; private set; }

        internal static string GetContentFolderNamespace(string contentFolder)
        {
            return String.Format("{0}.Content.{1}", typeof(GlobalDashboardRoutes).Namespace, contentFolder);
        }

        internal static string GetContentResourceName(string contentFolder, string resourceName)
        {
            return String.Format("{0}.{1}", GetContentFolderNamespace(contentFolder), resourceName);
        }
    }
}
