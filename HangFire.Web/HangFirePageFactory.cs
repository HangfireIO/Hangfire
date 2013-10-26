using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

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
                "/queues/dequeued/(?<Queue>.+)",
                x => new DequeuedJobsPage(x.Groups["Queue"].Value));
            RegisterPathHandlerFactory(
                "/queues/(?<Queue>.+)",
                x => new EnqueuedJobsPage(x.Groups["Queue"].Value));

            RegisterPathHandlerFactory("/processing", x => new ProcessingJobsPage());
            RegisterPathHandlerFactory("/scheduled", x => new ScheduledJobsPage());

            RegisterPathHandlerFactory(
                "/schedule/enqueue/(?<JobId>.+)",
                x => new CommandHandler(() => Command.EnqueueScheduled(x.Groups["JobId"].Value)));

            RegisterPathHandlerFactory("/servers", x => new ServersPage());
            RegisterPathHandlerFactory("/succeeded", x => new SucceededJobs());
            RegisterPathHandlerFactory("/failed", x => new FailedJobsPage());

            RegisterPathHandlerFactory(
                "/failed/retry/(?<JobId>.+)", 
                x => new CommandHandler(() => Command.Retry(x.Groups["JobId"].Value)));

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

            context.Items.Add("GenerationStartedAt", DateTime.UtcNow);

            var request = context.Request;
            var resource = request.PathInfo.Length == 0
                ? String.Empty
                : request.PathInfo.ToLowerInvariant();
            
            var handler = FindHandler(resource);
            if (handler == null)
            {
                throw new HttpException(404, "Resource not found.");
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
