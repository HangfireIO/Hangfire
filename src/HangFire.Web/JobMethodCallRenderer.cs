// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using HangFire.Common;

namespace HangFire.Web
{
    internal static class JobMethodCallRenderer
    {
        public static IHtmlString Render(Job job)
        {
            if (job == null) { return new HtmlString("<em>Can not find the target method.</em>"); }

            var builder = new StringBuilder();

            builder.Append(WrapKeyword("using"));
            builder.Append(" ");
            builder.Append(Encode(job.Type.Namespace));
            builder.Append(";");
            builder.AppendLine();
            builder.AppendLine();

            if (!job.Method.IsStatic)
            {
                var serviceName = Char.ToLower(job.Type.Name[0]) + job.Type.Name.Substring(1);

                builder.Append(WrapType(job.Type.Name));
                builder.AppendFormat(
                    " {0} = Activate<{1}>();",
                    Encode(serviceName),
                    WrapType(Encode(job.Type.Name)));

                builder.AppendLine();

                builder.Append(Encode(serviceName));
            }
            else
            {
                builder.Append(WrapType(Encode(job.Type.Name)));
            }

            builder.Append(".");
            builder.Append(Encode(job.Method.Name));
            builder.Append("(");

            var parameters = job.Method.GetParameters();
            var renderedArguments = new List<string>(parameters.Length);
            var renderedArgumentsTotalLength = 0;

            const int splitStringMinLength = 200;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (i < job.Arguments.Length)
                {
                    var argument = job.Arguments[i]; // TODO: check bounds

                    var argumentRenderer = ArgumentRenderer.GetRenderer(parameter.ParameterType);

                    var renderedArgument = argumentRenderer.Render(parameter.Name, argument);
                    renderedArguments.Add(renderedArgument);
                    renderedArgumentsTotalLength += renderedArgument.Length;
                }
                else
                {
                    renderedArguments.Add(Encode("<NO VALUE>"));
                }
            }

            for (int i = 0; i < renderedArguments.Count; i++)
            {
                var renderedArgument = renderedArguments[i];
                if (renderedArgumentsTotalLength > splitStringMinLength)
                {
                    builder.AppendLine();
                    builder.Append("    ");
                }
                else if (i > 0)
                {
                    builder.Append(" ");
                }

                builder.Append(renderedArgument);

                if (i < renderedArguments.Count - 1)
                {
                    builder.Append(",");
                }
            }

            builder.Append(");");

            return new HtmlString(builder.ToString());
        }

        private static string WrapIdentifier(string value)
        {
            return value;
        }

        private static string WrapKeyword(string value)
        {
            return Span("keyword", value);
        }

        private static string WrapType(string value)
        {
            return Span("type", value);
        }

        private static string WrapString(string value)
        {
            return Span("string", value);
        }

        private static string WrapComment(string value)
        {
            return Span("comment", value);
        }

        private static string Span(string @class, string value)
        {
            return String.Format(
                "<span class=\"{0}\">{1}</span>", 
                HttpUtility.HtmlAttributeEncode(@class), 
                value);
        }

        private static string Encode(string value)
        {
            return HttpUtility.HtmlEncode(value);
        }

        private class ArgumentRenderer
        {
            private string _enclosingString;
            private Type _deserializationType;
            private Func<string, string> _valueRenderer;

            private ArgumentRenderer()
            {
                _enclosingString = "\"";
                _valueRenderer = WrapString;
            }

            public string Render(string name, string value)
            {
                if (value == null)
                {
                    return WrapKeyword("null");
                }

                var builder = new StringBuilder();

                if (name != null)
                {
                    builder.AppendFormat(
                        "<span title=\"{0}:\" data-placement=\"left\">", 
                        HttpUtility.HtmlAttributeEncode(name));
                }

                if (_deserializationType != null)
                {
                    builder.Append(WrapIdentifier(
                        String.Format("Deserialize<{0}>(", WrapType(_deserializationType.Name))));
                }

                builder.Append(_valueRenderer(Encode(_enclosingString + value + _enclosingString)));

                if (_deserializationType != null)
                {
                    builder.Append(")");
                }

                if (name != null)
                {
                    builder.Append("</span>");
                }

                return builder.ToString();
            }

            public static ArgumentRenderer GetRenderer(Type type)
            {
                if (IsNumericType(type))
                {
                    return new ArgumentRenderer
                    {
                        _enclosingString = String.Empty,
                        _valueRenderer = WrapIdentifier
                    };
                }

                if (type == typeof(bool))
                {
                    return new ArgumentRenderer
                    {
                        _valueRenderer = WrapKeyword,
                        _enclosingString = String.Empty,
                    };
                }

                if (type == typeof(char))
                {
                    return new ArgumentRenderer
                    {
                        _enclosingString = "'",
                    };
                }

                if (type == typeof(string) || type == typeof(object))
                {
                    return new ArgumentRenderer
                    {
                        _enclosingString = "\"",
                    };
                }

                return new ArgumentRenderer
                {
                    _deserializationType = type,
                };
            }

            private static bool IsNumericType(Type type)
            {
                if (type == null) return false;

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Byte:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;

                    case TypeCode.Object:
                        if (IsNullableType(type))
                        {
                            return IsNumericType(Nullable.GetUnderlyingType(type));
                        }
                        return false;
                }
                return false;
            }

            private static bool IsNullableType(Type type)
            {
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            }
        }
    }
}
