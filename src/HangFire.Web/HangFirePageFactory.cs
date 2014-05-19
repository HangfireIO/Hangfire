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
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using HangFire.States;
using HangFire.Web.Configuration;
using HangFire.Web.Pages;

namespace HangFire.Web
{
    internal class HangFirePageFactory : IHttpHandlerFactory
    {
        private static readonly List<Tuple<string, Func<Match, IHttpHandler>>> PathHandlerFactories
            = new List<Tuple<string, Func<Match, IHttpHandler>>>();

        private static Func<IHttpHandler> _defaultHandlerFactory;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static HangFirePageFactory()
        {
            RazorPage.ExceptionHandler = ex => new ErrorPage(ex);

            RegisterDefaultPathHandlerFactory(() => new DashboardPage());

            RegisterPathHandlerFactory(
                "/job/(?<JobId>.+)",
                x => new JobDetailsPage(x.Groups["JobId"].Value));

            RegisterPathHandlerFactory("/queues", x => new QueuesPage());
            RegisterPathHandlerFactory(
                "/queues/fetched/(?<Queue>.+)",
                x => new FetchedJobsPage(x.Groups["Queue"].Value));
            RegisterPathHandlerFactory(
                "/queues/(?<Queue>.+)",
                x => new EnqueuedJobsPage(x.Groups["Queue"].Value));

            RegisterPathHandlerFactory("/processing", x => new ProcessingJobsPage());
            RegisterPathHandlerFactory("/scheduled", x => new ScheduledJobsPage());

            RegisterPathHandlerFactory(
                "/schedule/enqueue/(?<JobId>.+)",
                x => new CommandHandler(() => Command.EnqueueScheduled(x.Groups["JobId"].Value)));

            RegisterPathHandlerFactory(
                "/schedule/delete/(?<JobId>.+)",
                x => new CommandHandler(() => BackgroundJob.Delete(x.Groups["JobId"].Value)));

            RegisterPathHandlerFactory("/servers", x => new ServersPage());
            RegisterPathHandlerFactory("/succeeded", x => new SucceededJobs());
            RegisterPathHandlerFactory("/failed", x => new FailedJobsPage());

            RegisterPathHandlerFactory("/failed/requeue", x => new BatchCommandHandler(
                jobId => BackgroundJob.Requeue(jobId, FailedState.StateName)));
            
            RegisterPathHandlerFactory("/failed/delete", x => new BatchCommandHandler(
                jobId => BackgroundJob.Delete(jobId, FailedState.StateName)));

            RegisterPathHandlerFactory("/deleted", x => new DeletedJobsPage());

            RegisterPathHandlerFactory("/deleted/requeue", x => new BatchCommandHandler(
                jobId => BackgroundJob.Requeue(jobId, DeletedState.StateName)));

            RegisterPathHandlerFactory(
                "/actions/requeue/(?<JobId>.+)",
                x => new CommandHandler(() => BackgroundJob.Requeue(x.Groups["JobId"].Value)));

            RegisterPathHandlerFactory(
                "/actions/delete/(?<JobId>.+)",
                x => new CommandHandler(() => BackgroundJob.Delete(x.Groups["JobId"].Value)));

            RegisterPathHandlerFactory("/js/scripts.js",  x => new JavaScriptHandler());
            RegisterPathHandlerFactory("/css/styles.css", x => new StyleSheetHandler());

            RegisterPathHandlerFactory(
                "/fonts/(?<File>.+)",
                x => new FontsHandler(x.Groups["File"].Value));

            RegisterPathHandlerFactory(
                "/stats",
                x => new JsonStats());
        }

        public static void RegisterPathHandlerFactory(
            string pathPattern, Func<Match, IHttpHandler> handlerFactory)
        {
            PathHandlerFactories.Add(new Tuple<string, Func<Match, IHttpHandler>>(
                pathPattern, handlerFactory));
        }

        public static void RegisterDefaultPathHandlerFactory(Func<IHttpHandler> handlerFactory)
        {
            _defaultHandlerFactory = handlerFactory;
        }

        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            if (context == null) throw new ArgumentNullException("context");

            if (!HangFireConfiguration.EnableRemoteMonitorAccess && !context.Request.IsLocal)
            {
                return HttpStatusHandler.Process(context, HttpStatusCode.Unauthorized);
            }

            context.Items.Add("GenerationStartedAt", DateTime.UtcNow);

            var request = context.Request;
            var resource = request.PathInfo.Length == 0
                ? String.Empty
                : request.PathInfo.ToLowerInvariant();
            
            var handler = FindHandler(resource);
            if (handler == null)
            {
                return HttpStatusHandler.Process(context, HttpStatusCode.NotFound);
            }

            return handler;
        }

        private static IHttpHandler FindHandler(string resource)
        {
            if (resource.Length == 0 || resource.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                return _defaultHandlerFactory();
            }

            foreach (var pathHandlerFactory in PathHandlerFactories)
            {
                var pattern = pathHandlerFactory.Item1;

                if (!pattern.StartsWith("^", StringComparison.OrdinalIgnoreCase)) 
                    pattern = "^" + pattern;
                if (!pattern.EndsWith("$", StringComparison.OrdinalIgnoreCase)) 
                    pattern += "$";

                var match = Regex.Match(
                    resource,
                    pattern, 
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (match.Success)
                {
                    return pathHandlerFactory.Item2(match);
                }
            }

            return null;
        }

        internal static string GetContentFolderNamespace(string contentFolder)
        {
            return String.Format("{0}.Content.{1}", typeof (HangFirePageFactory).Namespace, contentFolder);
        }

        internal static string GetContentResourceName(string contentFolder, string resourceName)
        {
            return String.Format("{0}.{1}", GetContentFolderNamespace(contentFolder), resourceName);
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
        }
    }
}
