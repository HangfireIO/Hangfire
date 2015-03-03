// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Net;
using System.Text;
using Microsoft.Owin;

namespace Hangfire.Dashboard
{
    public abstract class RazorPage 
    {
        public static Func<Exception, RazorPage> ExceptionHandler;

        private readonly StringBuilder _content = new StringBuilder();
        private string _innerContent;

        public RazorPage Layout { get; protected set; }
        public JobStorage Storage { get; internal set; }
        public string AppPath { get; internal set; }

        internal IOwinRequest Request { private get; set; }
        internal IOwinResponse Response { private get; set; }

        public string RequestPath
        {
            get { return Request.Path.Value; }
        }

        public abstract void Execute();

        public string Query(string key)
        {
            return Request.Query[key];
        }

        public string LinkTo(string relativeUrl)
        {
            return Request.PathBase + relativeUrl;
        }

        public string TransformText()
        {
            return TransformText(null);
        }

        public string TransformText(string innerContent)
        {
            _innerContent = innerContent;

            Execute();

            if (Layout != null)
            {
                Layout.Request = Request;
                Layout.Response = Response;
                Layout.Storage = Storage;
                Layout.AppPath = AppPath;

                return Layout.TransformText(_content.ToString());
            }

            return _content.ToString();
        }

        protected void WriteLiteral(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
                return;
            _content.Append(textToAppend);
        }

        protected virtual void Write(object value)
        {
            if (value == null)
                return;
            var html = value as NonEscapedString;
            WriteLiteral(html != null ? html.ToString() : Encode(value.ToString()));
        }

        protected virtual object RenderBody()
        {
            return new NonEscapedString(_innerContent);
        }

        protected NonEscapedString RenderPartial(RazorPage page)
        {
            page.Request = Request;
            page.Response = Response;
            page.Storage = Storage;
            page.AppPath = AppPath;

            page.Execute();
            return new NonEscapedString(page._content.ToString());
        }

        private string Encode(string text)
        {
            return string.IsNullOrEmpty(text)
                       ? string.Empty
                       : WebUtility.HtmlEncode(text);
        }
    }
}
