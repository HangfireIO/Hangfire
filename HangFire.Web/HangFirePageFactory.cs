using System;
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
            return resource.Length == 0 ? new DashboardPage() : null;
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
        }
    }
}
