using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Web;

using HangFire.Web.Content;

namespace HangFire.Web
{
    static class ManifestResourceHandler
    {
        public static Action<HttpContextBase> Create(string resourceName, string mediaType)
        {
            return Create(resourceName, mediaType, null);
        }

        public static Action<HttpContextBase> Create(string resourceName, string mediaType, Encoding responseEncoding)
        {
            return Create(new[] { resourceName }, mediaType, responseEncoding, false);
        }

        public static Action<HttpContextBase> Create(IEnumerable<string> resourceNames, string mediaType)
        {
            return Create(resourceNames, mediaType, null, false);
        }

        public static Action<HttpContextBase> Create(IEnumerable<string> resourceNames, string mediaType, Encoding responseEncoding, bool cacheResponse)
        {
            Debug.Assert(resourceNames != null);
            Debug.Assert(!String.IsNullOrEmpty(mediaType));

            return context =>
            {
                //
                // Set the response headers for indicating the content type 
                // and encoding (if specified).
                //

                var response = context.Response;
                response.ContentType = mediaType;

                if (cacheResponse)
                {
                    response.Cache.SetCacheability(HttpCacheability.Public);
                    response.Cache.SetExpires(DateTime.MaxValue);
                }

                if (responseEncoding != null)
                    response.ContentEncoding = responseEncoding;

                foreach (var resourceName in resourceNames)
                    ManifestResourceHelper.WriteResourceToStream(response.OutputStream, resourceName);
            };
        }
    }
}
