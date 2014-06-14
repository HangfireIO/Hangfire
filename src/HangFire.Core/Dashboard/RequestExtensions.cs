using Microsoft.Owin;

namespace HangFire.Dashboard
{
    public static class RequestExtensions
    {
        public static string LinkTo(this IOwinRequest request, string link)
        {
            return request.PathBase + link;
        }
    }
}
