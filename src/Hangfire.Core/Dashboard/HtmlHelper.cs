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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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

        public NonEscapedString StackTrace(string stackTrace)
        {
            using (var writer = new StringWriter())
            {
                MarkupStackTrace(stackTrace, writer);
                return new NonEscapedString(writer.ToString());
            }
        }

        // This stack trace highlighting code was derived from the project ELMAH (Error 
        // Logging Modules and Handlers for ASP.NET, https://code.google.com/p/elmah/),
        // licensed under the Apache License 2.0.
        // Copyright (c) 2004-9 Atif Aziz (http://www.raboof.com). All rights reserved.
        private static readonly Regex ReStackTrace = new Regex(@"
                ^
                \s*
                \w+ \s+ 
                (?<type> .+ ) \.
                (?<method> .+? ) 
                (?<params> \( (?<params> .*? ) \) )
                ( \s+ 
                \w+ \s+ 
                  (?<file> [a-z] \: .+? ) 
                  \: \w+ \s+ 
                  (?<line> [0-9]+ ) \p{P}? )?
                \s*
                $",
            RegexOptions.IgnoreCase
            | RegexOptions.Multiline
            | RegexOptions.ExplicitCapture
            | RegexOptions.CultureInvariant
            | RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Compiled);

        private static void MarkupStackTrace(string text, TextWriter writer)
        {
            Debug.Assert(text != null);
            Debug.Assert(writer != null);

            int anchor = 0;

            foreach (Match match in ReStackTrace.Matches(text))
            {
                HtmlEncode(text.Substring(anchor, match.Index - anchor), writer);
                MarkupStackFrame(text, match, writer);
                anchor = match.Index + match.Length;
            }

            HtmlEncode(text.Substring(anchor), writer);
        }

        private static void MarkupStackFrame(string text, Match match, TextWriter writer)
        {
            Debug.Assert(text != null);
            Debug.Assert(match != null);
            Debug.Assert(writer != null);

            int anchor = match.Index;
            GroupCollection groups = match.Groups;

            //
            // Type + Method
            //

            Group type = groups["type"];
            HtmlEncode(text.Substring(anchor, type.Index - anchor), writer);
            anchor = type.Index;
            writer.Write("<span class='st-frame'>");
            anchor = StackFrameSpan(text, anchor, "st-type", type, writer);
            anchor = StackFrameSpan(text, anchor, "st-method", groups["method"], writer);

            //
            // Parameters
            //

            Group parameters = groups["params"];
            HtmlEncode(text.Substring(anchor, parameters.Index - anchor), writer);
            writer.Write("<span class='st-params'>(");
            int position = 0;
            foreach (string parameter in parameters.Captures[0].Value.Split(','))
            {
                int spaceIndex = parameter.LastIndexOf(' ');
                if (spaceIndex <= 0)
                {
                    Span(writer, "st-param", parameter.Trim());
                }
                else
                {
                    if (position++ > 0)
                        writer.Write(", ");
                    string argType = parameter.Substring(0, spaceIndex).Trim();
                    Span(writer, "st-param-type", argType);
                    writer.Write(' ');
                    string argName = parameter.Substring(spaceIndex + 1).Trim();
                    Span(writer, "st-param-name", argName);
                }
            }
            writer.Write(")</span>");
            anchor = parameters.Index + parameters.Length;

            //
            // File + Line
            //

            anchor = StackFrameSpan(text, anchor, "st-file", groups["file"], writer);
            anchor = StackFrameSpan(text, anchor, "st-line", groups["line"], writer);

            writer.Write("</span>");

            //
            // Epilogue
            //

            int end = match.Index + match.Length;
            HtmlEncode(text.Substring(anchor, end - anchor), writer);
        }

        private static int StackFrameSpan(string text, int anchor, string klass, Group group, TextWriter writer)
        {
            Debug.Assert(text != null);
            Debug.Assert(group != null);
            Debug.Assert(writer != null);

            return group.Success
                 ? StackFrameSpan(text, anchor, klass, group.Value, group.Index, group.Length, writer)
                 : anchor;
        }

        private static int StackFrameSpan(string text, int anchor, string klass, string value, int index, int length, TextWriter writer)
        {
            Debug.Assert(text != null);
            Debug.Assert(writer != null);

            HtmlEncode(text.Substring(anchor, index - anchor), writer);
            Span(writer, klass, value);
            return index + length;
        }

        private static void Span(TextWriter writer, string klass, string value)
        {
            Debug.Assert(writer != null);

            writer.Write("<span class='");
            writer.Write(klass);
            writer.Write("'>");
            HtmlEncode(value, writer);
            writer.Write("</span>");
        }

        private static void HtmlEncode(string text, TextWriter writer)
        {
            Debug.Assert(writer != null);
            WebUtility.HtmlEncode(text, writer);
        }

        public string HtmlEncode(string text)
        {
            return WebUtility.HtmlEncode(text);
        }
    }
}
