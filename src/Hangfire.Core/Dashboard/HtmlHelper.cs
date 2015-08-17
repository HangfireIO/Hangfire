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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Hangfire.Common;
using System.ComponentModel;
using Hangfire.Annotations;
using Hangfire.Dashboard.Pages;

namespace Hangfire.Dashboard
{
    public class HtmlHelper
    {
        private readonly RazorPage _page;

        public HtmlHelper([NotNull] RazorPage page)
        {
            if (page == null) throw new ArgumentNullException("page");
            _page = page;
        }

        public NonEscapedString Breadcrumbs(string title, [NotNull] IDictionary<string, string> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            return RenderPartial(new Breadcrumbs(title, items));
        }

        public NonEscapedString JobsSidebar()
        {
            return RenderPartial(new SidebarMenu(JobsSidebarMenu.Items));
        }

        public NonEscapedString SidebarMenu([NotNull] IEnumerable<Func<RazorPage, MenuItem>> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            return RenderPartial(new SidebarMenu(items));
        }

        public NonEscapedString BlockMetric([NotNull] DashboardMetric metric)
        {
            if (metric == null) throw new ArgumentNullException("metric");
            return RenderPartial(new BlockMetric(metric));
        }

        public NonEscapedString InlineMetric([NotNull] DashboardMetric metric)
        {
            if (metric == null) throw new ArgumentNullException("metric");
            return RenderPartial(new InlineMetric(metric));
        }

        public NonEscapedString Paginator([NotNull] Pager pager)
        {
            if (pager == null) throw new ArgumentNullException("pager");
            return RenderPartial(new Paginator(pager));
        }

        public NonEscapedString PerPageSelector([NotNull] Pager pager)
        {
            if (pager == null) throw new ArgumentNullException("pager");
            return RenderPartial(new PerPageSelector(pager));
        }

        public NonEscapedString RenderPartial(RazorPage partialPage)
        {
            partialPage.Assign(_page);
            return new NonEscapedString(partialPage.ToString());
        }

        public NonEscapedString Raw(string value)
        {
            return new NonEscapedString(value);
        }

        public NonEscapedString JobId(string jobId, bool shorten = true)
        {
            Guid guid;
            return new NonEscapedString(Guid.TryParse(jobId, out guid)
                ? (shorten ? jobId.Substring(0, 8) + "…" : jobId)
                : "#" + jobId);
        }

        public string JobName(Job job)
        {
            if (job == null)
            {
                return "Can not find the target method.";
            }

            var displayNameAttribute = Attribute.GetCustomAttribute(job.Method, typeof(DisplayNameAttribute), true) as DisplayNameAttribute;

            if (displayNameAttribute == null || displayNameAttribute.DisplayName == null)
            {
                return job.ToString();
            }

            try
            {
                var arguments = job.Arguments.Cast<object>().ToArray();
                return String.Format(displayNameAttribute.DisplayName, arguments);
            }
            catch (FormatException)
            {
                return displayNameAttribute.DisplayName;
            }
        }

        public NonEscapedString StateLabel(string stateName)
        {
            if (String.IsNullOrWhiteSpace(stateName))
            {
                return Raw("<em>No state</em>");
            }

            return Raw(String.Format(
                "<span class=\"label label-default\" style=\"background-color: {0};\">{1}</span>",
                JobHistoryRenderer.GetForegroundStateColor(stateName),
                stateName));
        }

        public NonEscapedString JobIdLink(string jobId)
        {
            return Raw(String.Format("<a href=\"{0}\">{1}</a>", 
                _page.Url.JobDetails(jobId), 
                JobId(jobId)));
        }

        public NonEscapedString JobNameLink(string jobId, Job job)
        {
            return Raw(String.Format(
                "<a class=\"job-method\" href=\"{0}\">{1}</a>",
                _page.Url.JobDetails(jobId),
                 HtmlEncode(JobName(job))));
        }

        public NonEscapedString RelativeTime(DateTime value)
        {
            return Raw(String.Format(
                "<span data-moment=\"{0}\">{1}</span>",
                JobHelper.ToTimestamp(value),
                value));
        }

        public string ToHumanDuration(TimeSpan? duration, bool displaySign = true)
        {
            if (duration == null) return null;

            var builder = new StringBuilder();
            if (displaySign)
            {
                builder.Append(duration.Value.TotalMilliseconds < 0 ? "-" : "+");
            }

            duration = duration.Value.Duration();

            if (duration.Value.Days > 0)
            {
                builder.AppendFormat("{0}d ", duration.Value.Days);
            }

            if (duration.Value.Hours > 0)
            {
                builder.AppendFormat("{0}h ", duration.Value.Hours);
            }

            if (duration.Value.Minutes > 0)
            {
                builder.AppendFormat("{0}m ", duration.Value.Minutes);
            }

            if (duration.Value.TotalHours < 1)
            {
                if (duration.Value.Seconds > 0)
                {
                    builder.Append(duration.Value.Seconds);
                    if (duration.Value.Milliseconds > 0)
                    {
                        builder.AppendFormat(".{0}", duration.Value.Milliseconds);
                    }

                    builder.Append("s ");
                }
                else
                {
                    if (duration.Value.Milliseconds > 0)
                    {
                        builder.AppendFormat("{0}ms ", duration.Value.Milliseconds);
                    }
                }
            }

            if (builder.Length <= 1)
            {
                builder.Append(" <1ms ");
            }

            builder.Remove(builder.Length - 1, 1);

            return builder.ToString();
        }

        public string FormatProperties(IDictionary<string, string> properties)
        {
            return @String.Join(", ", properties.Select(x => String.Format("{0}: \"{1}\"", x.Key, x.Value)));
        }

        public NonEscapedString QueueLabel(string queue)
        {
            string label;
            if (queue != null)
            {
                label = "<span class=\"label label-queue label-primary\">" + queue + "</span>";
            }
            else
            {
                label = "<span class=\"label label-queue label-danger\"><i>Unknown</i></span>";
            }

            return new NonEscapedString(label);
        }

        static readonly StackTraceHtmlFragments StackTraceHtmlFragments = new StackTraceHtmlFragments
        {
            BeforeFrame         = "<span class='st-frame'>"                            , AfterFrame         = "</span>",
            BeforeType          = "<span class='st-type'>"                             , AfterType          = "</span>",
            BeforeMethod        = "<span class='st-method'>"                           , AfterMethod        = "</span>",
            BeforeParameters    = "<span class='st-param'>"                            , AfterParameters    = "</span>",
            BeforeParameterType = "<span class='st-param'><span class='st-param-type'>", AfterParameterType = "</span>",
            BeforeParameterName = "<span class='st-param-name'>"                       , AfterParameterName = "</span></span>",
            BeforeFile          = "<span class='st-file'>"                             , AfterFile          = "</span>",
            BeforeLine          = "<span class='st-line'>"                             , AfterLine          = "</span>",
        };

        public NonEscapedString StackTrace(string stackTrace)
        {
            return new NonEscapedString(StackTraceFormatter.FormatHtml(stackTrace, StackTraceHtmlFragments));
        }

        public string HtmlEncode(string text)
        {
            return WebUtility.HtmlEncode(text);
        }
    }
}
