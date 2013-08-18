using System;
using System.Text;
using System.Web;

namespace HangFire.Web
{
    public class HangFirePageFactory : IHttpHandlerFactory
    {
        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            var request = context.Request;
            var resource = request.PathInfo.Length == 0
                ? String.Empty
                : request.PathInfo.Substring(1).ToLowerInvariant();
            
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
                case "queues":
                    return new QueuesPage();
                case "dispatchers":
                    return new DispatchersPage();
                case "css":
                    return new DelegatingHttpHandler(ManifestResourceHandler.Create(StyleSheetHelper.StyleSheetResourceNames, "text/css", Encoding.GetEncoding("Windows-1252"), true));
                default:
                    return resource.Length == 0 ? new DashboardPage() : null;
            }
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
        }
    }
}
