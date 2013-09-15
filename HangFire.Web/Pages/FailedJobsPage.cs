using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace HangFire.Web.Pages
{
    partial class FailedJobsPage
    {
        public IHtmlString MarkupStackTrace(string stackTrace)
        {
            var writer = new StringWriter();
            MarkupStackTrace(stackTrace, writer);
            return new HtmlString(writer.ToString());
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

        public void MarkupStackTrace(string text, TextWriter writer)
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

        private void MarkupStackFrame(string text, Match match, TextWriter writer)
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

        private int StackFrameSpan(string text, int anchor, string klass, Group group, TextWriter writer)
        {
            Debug.Assert(text != null);
            Debug.Assert(group != null);
            Debug.Assert(writer != null);

            return group.Success
                 ? StackFrameSpan(text, anchor, klass, group.Value, group.Index, group.Length, writer)
                 : anchor;
        }

        private int StackFrameSpan(string text, int anchor, string klass, string value, int index, int length, TextWriter writer)
        {
            Debug.Assert(text != null);
            Debug.Assert(writer != null);

            HtmlEncode(text.Substring(anchor, index - anchor), writer);
            Span(writer, klass, value);
            return index + length;
        }

        private void Span(TextWriter writer, string klass, string value)
        {
            Debug.Assert(writer != null);

            writer.Write("<span class='");
            writer.Write(klass);
            writer.Write("'>");
            HtmlEncode(value, writer);
            writer.Write("</span>");
        }

        private void HtmlEncode(string text, TextWriter writer)
        {
            Debug.Assert(writer != null);
            Server.HtmlEncode(text, writer);
        }
    }
}
