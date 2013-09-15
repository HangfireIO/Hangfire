using System.Text;

namespace HangFire.Web
{
    internal class StyleSheetHandler : CombinedResourceHandler
    {
        private static readonly string[] Stylesheets = 
            new[]
            {
                "bootstrap.min.css", 
                "rickshaw.min.css", 
                "hangfire.css"
            };

        public StyleSheetHandler() 
            : base(typeof(StyleSheetHandler).Assembly, HangFirePageFactory.GetContentFolderNamespace("css"), Stylesheets)
        {
            ContentType = "text/css";
            ContentEncoding = Encoding.UTF8;
        }
    }
}
