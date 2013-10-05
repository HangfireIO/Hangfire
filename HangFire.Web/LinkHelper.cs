using System;
using System.Text;
using System.Web;

namespace HangFire.Web
{
    static class LinkHelper
    {
        public static string LinkTo(this HttpRequestBase request, string link)
        {
            var sb = new StringBuilder(request.Path);
            var pathInfo = request.PathInfo;
            var pathInfoIndex = pathInfo.Length - 1;
            var sbIndex = sb.Length - 1;
            while (pathInfoIndex >= 0 && sb[sbIndex].Equals(pathInfo[pathInfoIndex]))
            {
                sb.Remove(sbIndex, 1);
                sbIndex--;
                pathInfoIndex--;
            }
            var basePath = sb.ToString();
            if (!basePath.EndsWith("/", StringComparison.OrdinalIgnoreCase)) basePath += "/";

            return basePath + link.TrimStart('/');
        }
    }
}
