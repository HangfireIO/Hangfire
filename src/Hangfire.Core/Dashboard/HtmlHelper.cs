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

        public string DisplayJob(Job job)
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

        public NonEscapedString Raw(string value)
        {
            return new NonEscapedString(value);
        }

        public NonEscapedString JobId(string jobId, bool shorten = true)
        {
            Guid guid;
            return new NonEscapedString(Guid.TryParse(jobId, out guid)
                ? (shorten ? jobId.Substring(0, 8) : jobId)
                : "#" + jobId);
        }

        public string ToHumanDuration(TimeSpan? duration, bool displaySign = true)
        {
            if (duration == null) return null;
            return ToHumanDuration(duration.Value.TotalMilliseconds, displaySign);
        }

        public string ToHumanDuration(double? duration, bool displaySign = true)
        {
            if (duration == null) return null;

            var timespan = TimeSpan.FromMilliseconds(duration.Value);
            var builder = new StringBuilder();
            if (displaySign)
            {
                builder.Append(timespan.TotalMilliseconds < 0 ? "-" : "+");
            }

            timespan = timespan.Duration();

            if (timespan.Days > 0)
            {
                builder.AppendFormat("{0}d ", timespan.Days);
            }

            if (timespan.Hours > 0)
            {
                builder.AppendFormat("{0}h ", timespan.Hours);
            }

            if (timespan.Minutes > 0)
            {
                builder.AppendFormat("{0}m ", timespan.Minutes);
            }

            if (timespan.TotalHours < 1)
            {
                if (timespan.Seconds > 0)
                {
                    builder.Append(timespan.Seconds);
                    if (timespan.Milliseconds > 0)
                    {
                        builder.AppendFormat(".{0}", timespan.Milliseconds);
                    }

                    builder.Append("s ");
                }
                else
                {
                    if (timespan.Milliseconds > 0 && builder.Length > 1)
                    {
                        builder.AppendFormat("{0}ms ", timespan.Milliseconds);
                    }
                }
            }

            if (builder.Length <= 1)
            {
                builder.AppendFormat("{0:N} ms ", duration);
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

        public NonEscapedString MarkupStackTrace(string stackTrace)
        {
            using (var writer = new StringWriter())
            {
                MarkupStackTrace(stackTrace, writer);
                return new NonEscapedString(writer.ToString());
            }
        }

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
    }
}
