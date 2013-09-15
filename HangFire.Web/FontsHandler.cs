namespace HangFire.Web
{
    internal class FontsHandler : SingleResourceHandler
    {
        public FontsHandler(string fontName) 
            : base(typeof(FontsHandler).Assembly, HangFirePageFactory.GetContentResourceName("fonts", fontName))
        {
            CacheResponse = true;

            if (fontName.EndsWith(".eot"))
            {
                ContentType = "application/vnd.ms-fontobject";
            } 
            else if (fontName.EndsWith(".svg"))
            {
                ContentType = "image/svg+xml";
            }
            else if (fontName.EndsWith(".ttf"))
            {
                ContentType = "application/octet-stream";
            }
            else if (fontName.EndsWith(".woff"))
            {
                ContentType = "application/font-woff";
            }
        }
    }
}
