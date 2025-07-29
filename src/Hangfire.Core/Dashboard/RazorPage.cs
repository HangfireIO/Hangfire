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
using Hangfire.Annotations;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Dashboard
{
    public abstract class RazorPage
    {
        private Lazy<StatisticsDto>? _statisticsLazy;
        private Lazy<Tuple<DateTime?, DateTime, TimeSpan?>>? _dateTimeLazy;

        private readonly StringBuilder _content = new StringBuilder();
        private string? _body;

        protected RazorPage()
        {
            GenerationTime = Stopwatch.StartNew();
            Html = new HtmlHelper(this);
        }

        [CanBeNull]
        public RazorPage? Layout { get; protected set; }

        [NotNull]
        public HtmlHelper Html { get; private set; }

        [NotNull]
        public UrlHelper Url { get; private set; } = null!;

        [NotNull]
        public JobStorage Storage => Context.Storage;

        [CanBeNull]
        public string? AppPath => Context.Options.AppPath;

        [NotNull]
        public DashboardOptions DashboardOptions => Context.Options;

        [NotNull]
        public Stopwatch GenerationTime { get; private set; }

        public DateTime? StorageUtcNow
        {
            get
            {
                if (_dateTimeLazy == null) throw new InvalidOperationException("Page is not initialized.");
                return _dateTimeLazy.Value.Item1;
            }
        }

        public DateTime ApplicationUtcNow
        {
            get
            {
                if (_dateTimeLazy == null) throw new InvalidOperationException("Page is not initialized.");
                return _dateTimeLazy.Value.Item2;
            }
        }

        public TimeSpan? TimeDifference
        {
            get
            {
                if (_dateTimeLazy == null) throw new InvalidOperationException("Page is not initialized.");
                return _dateTimeLazy.Value.Item3;
            }
        }

        [NotNull]
        public StatisticsDto Statistics
        {
            get
            {
                if (_statisticsLazy == null) throw new InvalidOperationException("Page is not initialized.");
                return _statisticsLazy.Value;
            }
        }

        [NotNull]
        public DashboardContext Context { get; private set; } = null!;

        internal DashboardRequest Request => Context.Request;
        internal DashboardResponse Response => Context.Response;

        [CanBeNull]
        public string? RequestPath => Request.Path;

        public bool IsReadOnly => Context.IsReadOnly;
        
        /// <exclude />
        public abstract void Execute();

        [CanBeNull]
        public string? Query([NotNull] string key)
        {
            return Request.GetQuery(key);
        }

        public override string ToString()
        {
            return TransformText(null);
        }

        /// <exclude />
        public void Assign([NotNull] RazorPage parentPage)
        {
            if (parentPage == null) throw new ArgumentNullException(nameof(parentPage));

            Context = parentPage.Context;
            Url = parentPage.Url;

            GenerationTime = parentPage.GenerationTime;
            _statisticsLazy = parentPage._statisticsLazy;
            _dateTimeLazy = parentPage._dateTimeLazy;
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

            _dateTimeLazy = new Lazy<Tuple<DateTime?, DateTime, TimeSpan?>>(() =>
            {
                DateTime? storageUtcNow = null;
                TimeSpan? difference = null;

                if (Storage.HasFeature(JobStorageFeatures.Connection.GetUtcDateTime))
                {
                    using (var connection = Storage.GetReadOnlyConnection() as JobStorageConnection)
                    {
                        storageUtcNow = connection?.GetUtcDateTime();
                        
                    }
                }

                var applicationUtcNow = DateTime.UtcNow;

                if (storageUtcNow.HasValue)
                {
                    difference = applicationUtcNow - storageUtcNow;
                }

                return new Tuple<DateTime?, DateTime, TimeSpan?>(storageUtcNow, applicationUtcNow, difference);
            });
        }

        /// <exclude />
        protected void WriteLiteral([CanBeNull] string? textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend)) return;
            _content.Append(textToAppend);
        }

        /// <exclude />
        protected virtual void Write([CanBeNull] object? value)
        {
            if (value == null) return;
            var html = value as NonEscapedString;
            WriteLiteral(html?.ToString() ?? Encode(value.ToString()));
        }

        protected virtual object RenderBody()
        {
            return new NonEscapedString(_body);
        }

        private string TransformText(string? body)
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

        private static string Encode(string? text)
        {
            return string.IsNullOrEmpty(text)
                       ? string.Empty
                       : WebUtility.HtmlEncode(text);
        }
    }
}
