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
using System.Linq;
using System.Text;
using System.Web;
using HangFire.Common;
using ServiceStack.Text;

namespace HangFire.Web
{
    internal static class JobMethodCallRenderer
    {
        public static IHtmlString Render(
            JobMethod method, string[] arguments, IDictionary<string, string> oldArguments)
        {
            var builder = new StringBuilder();
            
            builder.Append(WrapKeyword("using"));
            builder.Append(" ");
            builder.Append(Encode(method.Type.Namespace));
            builder.Append(";");
            builder.AppendLine();
            builder.AppendLine();

            if (!method.Method.IsStatic)
            {
                var serviceName = Char.ToLower(method.Type.Name[0]) + method.Type.Name.Substring(1);

                builder.Append(WrapKeyword("var"));
                builder.AppendFormat(
                    " {0} = {1}.Current.Activate<{2}>();",
                    Encode(serviceName),
                    WrapType("JobActivator"),
                    WrapType(Encode(method.Type.Name)));

                builder.AppendLine();

                if (method.OldFormat && oldArguments.Count != 0)
                {
                    foreach (var argument in oldArguments)
                    {
                        builder.Append(Encode(serviceName));
                        builder.Append(".");
                        builder.Append(Encode(argument.Key));
                        builder.Append(" = ");

                        var propertyInfo = method.Type.GetProperty(argument.Key);
                        var propertyType = propertyInfo != null ? propertyInfo.PropertyType : null;

                        var argumentRenderer = ArgumentRenderer.GetRenderer(propertyType);
                        builder.Append(argumentRenderer.Render(null, argument.Value));
                        builder.Append(";");

                        if (propertyInfo == null)
                        {
                            builder.Append(" ");
                            builder.Append(WrapComment("// Warning: property is missing"));
                        }

                        builder.AppendLine();
                    }

                    builder.AppendLine();
                }

                builder.Append(Encode(serviceName));
            }
            else
            {
                builder.Append(WrapType(Encode(method.Type.Name)));
            }

            builder.Append(".");
            builder.Append(Encode(method.Method.Name));
            builder.Append("(");

            var parameters = method.Method.GetParameters();
            
            if (!method.OldFormat)
            {
                var renderedArguments = new List<string>(parameters.Length);
                var renderedArgumentsTotalLength = 0;

                const int splitStringMinLength = 200;

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];

                    if (i < arguments.Length)
                    {
                        var argument = arguments[i]; // TODO: check bounds

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
                if (type.IsNumericType())
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
        }
    }
}
