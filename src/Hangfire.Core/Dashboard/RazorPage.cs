// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Diagnostics;
using System.Net;
using System.Text;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Dashboard
{
    public abstract class RazorPage
    {
        private Lazy<StatisticsDto> _statisticsLazy;

        private readonly StringBuilder _content = new StringBuilder();
        private string _body;

        protected RazorPage()
        {
            GenerationTime = Stopwatch.StartNew();
            Html = new HtmlHelper(this);
        }

        public RazorPage Layout { get; protected set; }
        public HtmlHelper Html { get; private set; }
        public UrlHelper Url { get; private set; }

        public JobStorage Storage => Context.Storage;
        public string AppPath => Context.Options.AppPath;
        public DashboardOptions DashboardOptions => Context.Options;
        public Stopwatch GenerationTime { get; private set; }

        public StatisticsDto Statistics
        {
            get
            {
                if (_statisticsLazy == null) throw new InvalidOperationException("Page is not initialized.");
                return _statisticsLazy.Value;
            }
        }

        public DashboardContext Context { get; private set; }

        internal DashboardRequest Request => Context.Request;
        internal DashboardResponse Response => Context.Response;

        public string RequestPath => Request.Path;

        public bool IsReadOnly => Context.IsReadOnly;
        
        /// <exclude />
        public abstract void Execute();

        public string Query(string key)
        {
            return Request.GetQuery(key);
        }

        public override string ToString()
        {
            return TransformText(null);
        }

        /// <exclude />
        public void Assign(RazorPage parentPage)
        {
            Context = parentPage.Context;
            Url = parentPage.Url;

            GenerationTime = parentPage.GenerationTime;
            _statisticsLazy = parentPage._statisticsLazy;
        }

        internal void Assign(DashboardContext context)
        {
            Context = context;
            Url = new UrlHelper(context);

            _statisticsLazy = new Lazy<StatisticsDto>(() =>
            {
                var monitoring = Storage.GetMonitoringApi();
                return monitoring.GetStatistics();
            });
        }

        /// <exclude />
        protected void WriteLiteral(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
                return;
            _content.Append(textToAppend);
        }

        /// <exclude />
        protected virtual void Write(object value)
        {
            if (value == null)
                return;
            var html = value as NonEscapedString;
            WriteLiteral(html?.ToString() ?? Encode(value.ToString()));
        }

        protected virtual object RenderBody()
        {
            return new NonEscapedString(_body);
        }

        private string TransformText(string body)
        {
            _body = body;
            
            Execute();
            
            if (Layout != null)
            {
                Layout.Assign(this);
                return Layout.TransformText(_content.ToString());
            }
            
            return _content.ToString();
        }

        private static string Encode(string text)
        {
            return string.IsNullOrEmpty(text)
                       ? string.Empty
                       : WebUtility.HtmlEncode(text);
        }
    }
}
