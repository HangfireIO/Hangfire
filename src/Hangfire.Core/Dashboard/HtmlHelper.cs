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
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Hangfire.Annotations;
using Hangfire.Dashboard.Pages;
using Hangfire.Dashboard.Resources;

namespace Hangfire.Dashboard
{
    public class HtmlHelper
    {
        private static readonly Type DisplayNameType;
        private static readonly Func<object, string> GetDisplayName;

        private readonly RazorPage _page;

        static HtmlHelper()
        {
            try
            {
#if NETFULL
                DisplayNameType = typeof(DisplayNameAttribute);
#else
                DisplayNameType = Type.GetType("System.ComponentModel.DisplayNameAttribute, System.ComponentModel.Primitives");
#endif
                if (DisplayNameType == null) return;

                var p = Expression.Parameter(typeof(object));
                var converted = Expression.Convert(p, DisplayNameType);

                GetDisplayName = Expression.Lambda<Func<object, string>>(Expression.Call(converted, "get_DisplayName", null), p).Compile();
            }
            catch
            {
                // Ignoring
            }
        }

        public HtmlHelper([NotNull] RazorPage page)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            _page = page;
        }

        public NonEscapedString Breadcrumbs(string title, [NotNull] IDictionary<string, string> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            return RenderPartial(new Breadcrumbs(title, items));
        }

        public NonEscapedString JobsSidebar()
        {
            return RenderPartial(new SidebarMenu(JobsSidebarMenu.Items));
        }

        public NonEscapedString SidebarMenu([NotNull] IEnumerable<Func<RazorPage, MenuItem>> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            return RenderPartial(new SidebarMenu(items));
        }

        public NonEscapedString BlockMetric([NotNull] DashboardMetric metric)
        {
            if (metric == null) throw new ArgumentNullException(nameof(metric));
            return RenderPartial(new BlockMetric(metric));
        }

        public NonEscapedString InlineMetric([NotNull] DashboardMetric metric)
        {
            if (metric == null) throw new ArgumentNullException(nameof(metric));
            return RenderPartial(new InlineMetric(metric));
        }

        public NonEscapedString Paginator([NotNull] Pager pager)
        {
            if (pager == null) throw new ArgumentNullException(nameof(pager));
            return RenderPartial(new Paginator(pager));
        }

        public NonEscapedString PerPageSelector([NotNull] Pager pager)
        {
            if (pager == null) throw new ArgumentNullException(nameof(pager));
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
                : $"#{jobId}");
        }

        public string JobName(Job job)
        {
            if (job == null)
            {
                return Strings.Common_CannotFindTargetMethod;
            }


            if (DisplayNameType != null && GetDisplayName != null)
            {
                var attribute = job.Method.GetCustomAttribute(DisplayNameType);
                if (attribute != null)
                {
                    try
                    {
                        return String.Format(GetDisplayName(attribute), job.Args.ToArray());
                    }
                    catch (FormatException)
                    {
                        return GetDisplayName(attribute);
                    }
                }
            }

            return job.ToString();
        }

        public NonEscapedString StateLabel(string stateName)
        {
            if (String.IsNullOrWhiteSpace(stateName))
            {
                return Raw($"<em>{Strings.Common_NoState}</em>");
            }

            return Raw($"<span class=\"label label-default\" style=\"background-color: {JobHistoryRenderer.GetForegroundStateColor(stateName)};\">{stateName}</span>");
        }

        public NonEscapedString JobIdLink(string jobId)
        {
            return Raw($"<a href=\"{_page.Url.JobDetails(jobId)}\">{JobId(jobId)}</a>");
        }

        public NonEscapedString JobNameLink(string jobId, Job job)
        {
            return Raw($"<a class=\"job-method\" href=\"{_page.Url.JobDetails(jobId)}\">{HtmlEncode(JobName(job))}</a>");
        }

        public NonEscapedString RelativeTime(DateTime value)
        {
            return Raw($"<span data-moment=\"{JobHelper.ToTimestamp(value)}\">{value}</span>");
        }

        public NonEscapedString MomentTitle(DateTime time, string value)
        {
            return Raw($"<span data-moment-title=\"{JobHelper.ToTimestamp(time)}\">{value}</span>");
        }

        public NonEscapedString LocalTime(DateTime value)
        {
            return Raw($"<span data-moment-local=\"{JobHelper.ToTimestamp(value)}\">{value}</span>");
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
                builder.Append($"{duration.Value.Days}d ");
            }

            if (duration.Value.Hours > 0)
            {
                builder.Append($"{duration.Value.Hours}h ");
            }

            if (duration.Value.Minutes > 0)
            {
                builder.Append($"{duration.Value.Minutes}m ");
            }

            if (duration.Value.TotalHours < 1)
            {
                if (duration.Value.Seconds > 0)
                {
                    builder.Append(duration.Value.Seconds);
                    if (duration.Value.Milliseconds > 0)
                    {
                        builder.Append($".{duration.Value.Milliseconds.ToString().PadLeft(3, '0')}");
                    }

                    builder.Append("s ");
                }
                else
                {
                    if (duration.Value.Milliseconds > 0)
                    {
                        builder.Append($"{duration.Value.Milliseconds}ms ");
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
            return String.Join(", ", properties.Select(x => $"{x.Key}: \"{x.Value}\""));
        }

        public NonEscapedString QueueLabel(string queue)
        {
            var label = queue != null 
                ? $"<a class=\"text-uppercase\" href=\"{_page.Url.Queue(queue)}\">{queue}</a>" 
                : $"<span class=\"label label-danger\"><i>{Strings.Common_Unknown}</i></span>";

            return new NonEscapedString(label);
        }

        public NonEscapedString ServerId(string serverId)
        {
            var parts = serverId.Split(':');
            var shortenedId = parts.Length > 1
                ? String.Join(":", parts.Take(parts.Length - 1))
                : serverId;

            return new NonEscapedString(
                $"<span class=\"labe label-defult text-uppercase\" title=\"{serverId}\">{shortenedId}</span>");
        }

        private static readonly StackTraceHtmlFragments StackTraceHtmlFragments = new StackTraceHtmlFragments
        {
            BeforeFrame         = "<span class='st-frame'>"                            , AfterFrame         = "</span>",
            BeforeType          = "<span class='st-type'>"                             , AfterType          = "</span>",
            BeforeMethod        = "<span class='st-method'>"                           , AfterMethod        = "</span>",
            BeforeParameters    = "<span class='st-params'>"                           , AfterParameters    = "</span>",
            BeforeParameterType = "<span class='st-param'><span class='st-param-type'>", AfterParameterType = "</span>",
            BeforeParameterName = "<span class='st-param-name'>"                       , AfterParameterName = "</span></span>",
            BeforeFile          = "<span class='st-file'>"                             , AfterFile          = "</span>",
            BeforeLine          = "<span class='st-line'>"                             , AfterLine          = "</span>",
        };

        public NonEscapedString StackTrace(string stackTrace)
        {
            try
            {
                return new NonEscapedString(StackTraceFormatter.FormatHtml(stackTrace, StackTraceHtmlFragments));
            }
            catch (RegexMatchTimeoutException)
            {
                return new NonEscapedString(HtmlEncode(stackTrace));
            }
        }

        public string HtmlEncode(string text)
        {
            return WebUtility.HtmlEncode(text);
        }
    }
}
