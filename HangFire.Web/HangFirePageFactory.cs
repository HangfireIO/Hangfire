using System;
using System.Text;
using System.Web;

using HangFire.Web.Content;
using HangFire.Web.Pages;

namespace HangFire.Web
{
    public class HangFirePageFactory : IHttpHandlerFactory
    {
        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
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

        public IHttpHandler FindHandler(string resource)
        {
            switch (resource)
            {
                case "/queues":
                    return new QueuesPage();
                case "/dispatchers":
                    return new DispatchersPage();
                case "/schedule":
                    return new SchedulePage();
                case "/servers":
                    return new ServersPage();
                case "/failed":
                    return new FailedJobsPage();
                case "/succeeded":
                    return new SucceededJobs();
                case "/scripts.js":
                    return new DelegatingHttpHandler(ManifestResourceHandler.Create(JavaScriptHelper.JavaScriptResourceNames, "application/javascript", Encoding.UTF8, false));
                case "/styles.css":
                    return new DelegatingHttpHandler(ManifestResourceHandler.Create(StyleSheetHelper.StyleSheetResourceNames, "text/css", Encoding.UTF8, false));
                case "/stats":
                    return new DelegatingHttpHandler(JsonStats.StatsResponse);
                case "/":
                    return new DashboardPage();
                default:
                    return resource.Length == 0 ? new DashboardPage() : null;
            }
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
        }
    }
}
