using System.Text;
using System.Web;

namespace HangFire.Web
{
    public abstract class RazorPage : GenericHandler
    {
        private readonly StringBuilder _content = new StringBuilder();
        private string _innerContent;

        public RazorPage Layout { get; set; }

        public abstract void Execute();

        public override void ProcessRequest()
        {
            Response.Write(TransformText(null));
        }

        public string TransformText(string innerContent)
        {
            _innerContent = innerContent;

            Execute();

            if (Layout != null)
            {
                return Layout.TransformText(_content.ToString());
            }

            return _content.ToString();
        }

        protected void WriteLiteral(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
                return;
            _content.Append(textToAppend); ;
        }

        protected virtual void Write(object value)
        {
            if (value == null)
                return;
            var html = value as IHtmlString;
            WriteLiteral(html != null ? html.ToHtmlString() : Encode(value.ToString()));
        }

        protected virtual object RenderBody()
        {
            return new HtmlString(_innerContent);
        }

        private string Encode(string text)
        {
            return string.IsNullOrEmpty(text)
                       ? string.Empty
                       : Server.HtmlEncode(text);
        }
    }
}
