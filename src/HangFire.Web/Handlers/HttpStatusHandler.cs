using System.Net;
using System.Web;

namespace HangFire.Web
{
    public static class HttpStatusHandler
    {
        public static IHttpHandler Process(HttpContext context, HttpStatusCode statusCode)
        {
            var response = context.Response;

            response.StatusCode = (int) statusCode;
            response.Write(response.Status);

            response.End();
            return null;
        }
    }
}
