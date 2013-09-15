using System.Text;

namespace HangFire.Web
{
    internal class JavaScriptHandler : CombinedResourceHandler
    {
        private static readonly string[] Javascripts =
            new[]
            {
                "jquery-1.10.2.min.js", 
                "d3.min.js", 
                "d3.layout.min.js", 
                "rickshaw.min.js", 
                "hangfire.js"
            };

        public JavaScriptHandler()
            : base(typeof(JavaScriptHandler).Assembly, HangFirePageFactory.GetContentFolderNamespace("js"), Javascripts)
        {
            ContentType = "application/javascript";
            ContentEncoding = Encoding.UTF8;
        }
    }
}
