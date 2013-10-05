using System;

namespace HangFire.Web
{
    internal class FontsHandler : SingleResourceHandler
    {
        public FontsHandler(string fontName) 
            : base(typeof(FontsHandler).Assembly, HangFirePageFactory.GetContentResourceName("fonts", fontName))
        {
            CacheResponse = true;

            if (fontName.EndsWith(".eot", StringComparison.OrdinalIgnoreCase))
            {
                ContentType = "application/vnd.ms-fontobject";
            } 
            else if (fontName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                ContentType = "image/svg+xml";
            }
            else if (fontName.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            {
                ContentType = "application/octet-stream";
            }
            else if (fontName.EndsWith(".woff", StringComparison.OrdinalIgnoreCase))
            {
                ContentType = "application/font-woff";
            }
        }
    }
}
