// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Text;
using System.Web;

namespace HangFire.Web
{
    internal abstract class RazorPage : GenericHandler
    {
        public static Func<Exception, RazorPage> ExceptionHandler;

        private readonly StringBuilder _content = new StringBuilder();
        private string _innerContent;

        public RazorPage Layout { get; set; }

        public abstract void Execute();

        public override void ProcessRequest()
        {
            string text;
            try
            {
                text = TransformText(null);
            }
            catch (Exception ex)
            {
                if (ExceptionHandler == null)
                {
                    throw;
                }

                var handler = ExceptionHandler(ex);
                text = handler.TransformText(null);
                Response.StatusCode = 500;
            }

            Response.Write(text);
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

        protected IHtmlString RenderPartial(RazorPage page)
        {
            page.Execute();
            return new HtmlString(page._content.ToString());
        }

        private string Encode(string text)
        {
            return string.IsNullOrEmpty(text)
                       ? string.Empty
                       : Server.HtmlEncode(text);
        }
    }
}
