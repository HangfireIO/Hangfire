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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HangFire.Common;
using HangFire.Common.States;
using ServiceStack.Text;

namespace HangFire.Web
{
    internal static class HtmlHelper
    {
        public static string DisplayMethod(JobMethod method)
        {
            if (method == null)
            {
                return null;
            }

            var separator = method.Method.IsStatic ? "." : "::";
            return String.Format("{0}{1}{2}", method.Type.Name, separator, method.Method.Name);
        }

        public static string DisplayMethodHint(JobMethod method)
        {
            return method == null ? null : method.Type.FullName;
        }

        public static IHtmlString Raw(string value)
        {
            return new HtmlString(value);
        }

        public static IHtmlString JobId(string jobId)
        {
            return new HtmlString(jobId.Substring(0, 8));
        }

        public static string JobType(JobMethod method)
        {
            if (method == null)
            {
                return "Could not find the target method.";
            }

            return method.Type.FullName;
        }

        public static string JobType(string typeName)
        {
            var type = Type.GetType(typeName, throwOnError: false);

            if (type == null)
            {
                return typeName;
            }

            return type.FullName;
        }

        public static string FormatJob(
            JobMethod method, string[] arguments, IDictionary<string, string> oldArguments)
        {
            var builder = new StringBuilder();
            var parameters = method.Method.GetParameters();

            if (parameters.Length > 0)
            {
                builder.AppendLine("/* Arrange */");
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var argument = arguments[i]; // TODO: check bounds

                builder.Append("var ");
                builder.Append(parameter.Name);
                builder.Append(" = ");

                if (!parameter.ParameterType.IsNumericType() && parameter.ParameterType != typeof(string))
                {
                    builder.AppendFormat("({0})", parameter.ParameterType.Name);
                }

                if (argument != null && !parameter.ParameterType.IsNumericType())
                {
                    builder.Append("\"");
                }

                if (argument == null)
                {
                    builder.Append("null");
                }
                else
                {
                    builder.Append(argument);
                }

                if (argument != null && !parameter.ParameterType.IsNumericType())
                {
                    builder.Append("\"");
                }

                builder.AppendLine(";");
            }

            if (parameters.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("/* Act */");
            }
            
            if (method.Method.IsStatic)
            {
                builder.AppendFormat(
                    "{0}.{1}.{2}({3});",
                    method.Type.Namespace,
                    method.Type.Name,
                    method.Method.Name,
                    String.Join(", ", parameters.Select(x => x.Name)));
            }
            else
            {
                var serviceName = Char.ToLower(method.Type.Name[0]) + method.Type.Name.Substring(1);

                builder.AppendFormat(
                    "var {0} = new {1}(/* ... */)",
                    serviceName,
                    method.Type.Name);

                if (!method.OldFormat || oldArguments.Count == 0)
                {
                    builder.AppendLine(";");
                }
                else
                {
                    builder.AppendLine();
                    builder.AppendLine("{");

                    foreach (var argument in oldArguments)
                    {
                        builder.AppendFormat("    {0} = \"{1}\",", argument.Key, argument.Value);
                        builder.AppendLine();
                    }

                    builder.AppendLine("}");
                    builder.AppendLine();
                }

                builder.AppendFormat(
                    "{0}.{1}({2});",
                    serviceName,
                    method.Method.Name,
                    String.Join(", ", parameters.Select(x => x.Name)));
            }

            return builder.ToString();
        }

        public static string FormatProperties(IDictionary<string, string> properties)
        {
            return @String.Join(", ", properties.Select(x => String.Format("{0}: \"{1}\"", x.Key, x.Value)));
        }

        public static IHtmlString QueueLabel(JobMethod method)
        {
            return QueueLabel(method.GetQueue());
        }

        public static IHtmlString QueueLabel(string queue)
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

            return new HtmlString(label);
        }

        public static IHtmlString MarkupStackTrace(string stackTrace)
        {
            using (var writer = new StringWriter())
            {
                MarkupStackTrace(stackTrace, writer);
                return new HtmlString(writer.ToString());
            }
        }

        private static readonly Regex _reStackTrace = new Regex(@"
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

            foreach (Match match in _reStackTrace.Matches(text))
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
            HttpUtility.HtmlEncode(text, writer);
        }
    }
}
