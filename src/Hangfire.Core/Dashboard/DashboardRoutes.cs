// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Reflection;
using Hangfire.Annotations;
using Hangfire.Dashboard.Pages;
using Hangfire.States;

namespace Hangfire.Dashboard
{
    /// <summary>
    /// Provides the routing mechanisms for the Dashboard UI. This class is used to register custom
    /// request dispatchers, allowing developers to write extensions for the Dashboard UI, such as
    /// custom pages, API endpoints or adding custom JavaScript or CSS files.
    /// </summary>
    /// <remarks>
    /// The <see cref="DashboardRoutes"/> class contains a collection of routes that the Dashboard UI uses to dispatch requests to handlers.
    /// Developers can use this class to add custom scripts, stylesheets, and register custom routes for extending the dashboard functionality.
    /// 
    /// To add a custom route, use the <see cref="DashboardRoutes.Routes"/> property which is an instance of <see cref="RouteCollection"/>.
    /// </remarks>
    /// <seealso cref="RouteCollection"/>
    /// <seealso cref="IDashboardDispatcher"/>
    public static class DashboardRoutes
    {
        private static readonly List<Tuple<Assembly, string>> JavaScripts = new List<Tuple<Assembly, string>>();
        private static readonly List<Tuple<Assembly, string>> Stylesheets = new List<Tuple<Assembly, string>>();
        private static readonly List<Tuple<Assembly, string>> StylesheetsDarkMode = new List<Tuple<Assembly, string>>();

        internal static volatile int JavaScriptsHashCode;
        internal static volatile int StylesheetsHashCode;
        internal static volatile int StylesheetsDarkModeHashCode;

        static DashboardRoutes()
        {
            Routes = new RouteCollection();
            Routes.AddRazorPage("/", static _ => new HomePage());
            Routes.Add("/stats", new JsonStats());

            var executingAssembly = typeof(DashboardRoutes).GetTypeInfo().Assembly;

            AddStylesheet(executingAssembly, GetContentResourceName("css", "bootstrap.min.css"));
            AddStylesheet(executingAssembly, GetContentResourceName("css", "Chart.min.css"));
            AddStylesheet(executingAssembly, GetContentResourceName("css", "hangfire.css"));

            AddStylesheetDarkMode(executingAssembly, GetContentResourceName("css", "hangfire-dark.css"));

            AddJavaScript(executingAssembly, GetContentResourceName("js", "jquery-3.7.1.min.js"));
            AddJavaScript(executingAssembly, GetContentResourceName("js", "bootstrap.min.js"));
            AddJavaScript(executingAssembly, GetContentResourceName("js", "moment-with-locales.min.js"));
            AddJavaScript(executingAssembly, GetContentResourceName("js", "Chart.min.js"));
            AddJavaScript(executingAssembly, GetContentResourceName("js", "chartjs-plugin-streaming.min.js"));
            AddJavaScript(executingAssembly, GetContentResourceName("js", "hangfire.js"));

            #region Embedded static content

            Routes.Add("/js[0-9]+", new CombinedResourceDispatcher("application/javascript", JavaScripts));
            Routes.Add("/css[0-9]+", new CombinedResourceDispatcher("text/css", Stylesheets));
            Routes.Add("/css-dark[0-9]+", new CombinedResourceDispatcher("text/css", StylesheetsDarkMode));

            Routes.Add("/fonts/glyphicons-halflings-regular/eot", new EmbeddedResourceDispatcher(
                "application/vnd.ms-fontobject",
                executingAssembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.eot")));

            Routes.Add("/fonts/glyphicons-halflings-regular/svg", new EmbeddedResourceDispatcher(
                "image/svg+xml",
                executingAssembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.svg")));

            Routes.Add("/fonts/glyphicons-halflings-regular/ttf", new EmbeddedResourceDispatcher(
                "application/octet-stream",
                executingAssembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.ttf")));

            Routes.Add("/fonts/glyphicons-halflings-regular/woff", new EmbeddedResourceDispatcher(
                "font/woff",
                executingAssembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.woff")));

            Routes.Add("/fonts/glyphicons-halflings-regular/woff2", new EmbeddedResourceDispatcher(
                "font/woff2",
                executingAssembly,
                GetContentResourceName("fonts", "glyphicons-halflings-regular.woff2")));

            #endregion

            #region Razor pages and commands

            Routes.AddRazorPage("/jobs/enqueued", static _ => new QueuesPage());
            Routes.AddRazorPage(
                "/jobs/enqueued/fetched/(?<Queue>.+)",
                static x => new FetchedJobsPage(x.Groups["Queue"].Value));

            Routes.AddClientBatchCommand("/jobs/enqueued/delete", static (client, jobId) => client.ChangeState(jobId, CreateDeletedState()));
            Routes.AddClientBatchCommand("/jobs/enqueued/requeue", static (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState()));

            Routes.AddRazorPage(
                "/jobs/enqueued/(?<Queue>.+)",
                static x => new EnqueuedJobsPage(x.Groups["Queue"].Value));

            Routes.AddRazorPage("/jobs/processing", static _ => new ProcessingJobsPage());
            Routes.AddClientBatchCommand(
                "/jobs/processing/delete", 
                static (client, jobId) => client.ChangeState(jobId, CreateDeletedState(), ProcessingState.StateName));

            Routes.AddClientBatchCommand(
                "/jobs/processing/requeue",
                static (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState(), ProcessingState.StateName));

            Routes.AddRazorPage("/jobs/scheduled", static _ => new ScheduledJobsPage());

            Routes.AddClientBatchCommand(
                "/jobs/scheduled/enqueue", 
                static (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState(), ScheduledState.StateName));

            Routes.AddClientBatchCommand(
                "/jobs/scheduled/delete",
                static (client, jobId) => client.ChangeState(jobId, CreateDeletedState(), ScheduledState.StateName));

            Routes.AddRazorPage("/jobs/succeeded", static _ => new SucceededJobs());
            Routes.AddClientBatchCommand(
                "/jobs/succeeded/requeue",
                static (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState(), SucceededState.StateName));

            Routes.AddRazorPage("/jobs/failed", static _ => new FailedJobsPage());

            Routes.AddClientBatchCommand(
                "/jobs/failed/requeue",
                static (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState(), FailedState.StateName));

            Routes.AddClientBatchCommand(
                "/jobs/failed/delete",
                static (client, jobId) => client.ChangeState(jobId, CreateDeletedState(), FailedState.StateName));

            Routes.AddRazorPage("/jobs/deleted", static _ => new DeletedJobsPage());

            Routes.AddClientBatchCommand(
                "/jobs/deleted/requeue",
                static (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState(), DeletedState.StateName));

            Routes.AddRazorPage("/jobs/awaiting", static _ => new AwaitingJobsPage());
            Routes.AddClientBatchCommand("/jobs/awaiting/enqueue", static (client, jobId) => client.ChangeState(
                jobId, CreateEnqueuedState(), AwaitingState.StateName));
            Routes.AddClientBatchCommand("/jobs/awaiting/delete", static (client, jobId) => client.ChangeState(
                jobId, CreateDeletedState(), AwaitingState.StateName));

            Routes.AddCommand(
                "/jobs/actions/requeue/(?<JobId>.+)",
                static context =>
                {
                    var client = context.GetBackgroundJobClient();
                    return client.ChangeState(context.UriMatch.Groups["JobId"].Value, CreateEnqueuedState());
                });

            Routes.AddCommand(
                "/jobs/actions/delete/(?<JobId>.+)",
                static context =>
                {
                    var client = context.GetBackgroundJobClient();
                    return client.ChangeState(context.UriMatch.Groups["JobId"].Value, CreateDeletedState());
                });

            Routes.AddRazorPage("/jobs/details/(?<JobId>.+)", static x => new JobDetailsPage(x.Groups["JobId"].Value));

            Routes.AddRazorPage("/recurring", x => new RecurringJobsPage());
            Routes.AddRecurringBatchCommand(
                "/recurring/remove", 
                static (manager, jobId) => manager.RemoveIfExists(jobId));

            Routes.AddRecurringBatchCommand(
                "/recurring/trigger", 
                static (manager, jobId) => manager.Trigger(jobId));
            
            Routes.AddRecurringBatchCommandV2(
                "/recurring/disable",
                static (manager, jobId) => manager.DisableIfExists(jobId));
            
            Routes.AddRecurringBatchCommandV2(
                "/recurring/enable",
                static (manager, jobId) => manager.EnableIfExists(jobId));

            Routes.AddRazorPage("/servers", static _ => new ServersPage());
            Routes.AddRazorPage("/retries", static _ => new RetriesPage());

            #endregion
        }

        /// <summary>
        /// Gets the collection of routes for the Dashboard UI. Use this property to register
        /// custom request dispatchers.
        /// </summary>
        public static RouteCollection Routes { get; }

        /// <summary>
        /// Adds a stylesheet resource embedded into the given assembly to be included in the dashboard.
        /// </summary>
        /// <remarks>
        /// The specified resource should be an embedded resource file within the referenced assembly.
        /// You can discover embedded resource names by calling the <c>assembly.GetManifestResourceNames()</c> method.
        /// </remarks>
        /// <param name="assembly">The assembly containing the embedded stylesheet resource.</param>
        /// <param name="resource">The name of the stylesheet embedded resource.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> or <paramref name="resource"/> is <c>null</c>.</exception>
        public static void AddStylesheet([NotNull] Assembly assembly, [NotNull] string resource)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            lock (Stylesheets)
            {
                Stylesheets.Add(Tuple.Create(assembly, resource));
                StylesheetsHashCode ^= resource.GetHashCode();
            }
        }

        /// <summary>
        /// Adds a resource embedded into the given assembly that will only be included in the dashboard
        /// when the <see cref="DashboardOptions.DarkModeEnabled"/> is set to <c>true</c>.
        /// </summary>
        /// <remarks>
        /// The specified resource should be an embedded resource file within the referenced assembly.
        /// You can discover embedded resource names by calling the <c>assembly.GetManifestResourceNames()</c> method.
        /// </remarks>
        /// <param name="assembly">The assembly containing the dark-mode stylesheet embedded resource.</param>
        /// <param name="resource">The name of the dark-mode stylesheet embedded resource.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> or <paramref name="resource"/> is <c>null</c>.</exception>
        public static void AddStylesheetDarkMode([NotNull] Assembly assembly, [NotNull] string resource)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            lock (StylesheetsDarkMode)
            {
                StylesheetsDarkMode.Add(Tuple.Create(assembly, resource));
                StylesheetsDarkModeHashCode ^= resource.GetHashCode();
            }
        }

        /// <summary>
        /// Adds a JavaScript resource embedded into the given assembly to be included in the dashboard.
        /// </summary>
        /// <remarks>
        /// The specified resource should be an embedded resource file within the referenced assembly.
        /// You can discover embedded resource names by calling the <c>assembly.GetManifestResourceNames()</c> method.
        /// </remarks>
        /// <param name="assembly">The assembly containing the JavaScript embedded resource.</param>
        /// <param name="resource">The name of the JavaScript embedded resource.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> or <paramref name="resource"/> is <c>null</c>.</exception>
        public static void AddJavaScript([NotNull] Assembly assembly, [NotNull] string resource)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            lock (JavaScripts)
            {
                JavaScripts.Add(Tuple.Create(assembly, resource));
                JavaScriptsHashCode ^= resource.GetHashCode();
            }
        }

        internal static string GetContentFolderNamespace(string contentFolder)
        {
            return $"{typeof (DashboardRoutes).Namespace}.Content.{contentFolder}";
        }

        internal static string GetContentResourceName(string contentFolder, string resourceName)
        {
            return $"{GetContentFolderNamespace(contentFolder)}.{resourceName}";
        }

        private static DeletedState CreateDeletedState()
        {
            return new DeletedState { Reason = "Triggered via Dashboard UI" };
        }

        private static EnqueuedState CreateEnqueuedState()
        {
            return new EnqueuedState { Reason = "Triggered via Dashboard UI" };
        }
    }
}
