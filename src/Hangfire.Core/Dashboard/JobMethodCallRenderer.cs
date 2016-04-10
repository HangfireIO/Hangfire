﻿// This file is part of Hangfire.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Hangfire.Common;

namespace Hangfire.Dashboard
{
    internal static class JobMethodCallRenderer
    {
        public static NonEscapedString Render(Job job)
        {
            if (job == null) { return new NonEscapedString("<em>Can not find the target method.</em>"); }

            var builder = new StringBuilder();

            builder.Append(WrapKeyword("using"));
            builder.Append(" ");
            builder.Append(Encode(job.Type.Namespace));
            builder.Append(";");
            builder.AppendLine();
            builder.AppendLine();

            string serviceName = null;

            if (!job.Method.IsStatic)
            {
                serviceName = GetNameWithoutGenericArity(job.Type);

                if (job.Type.GetTypeInfo().IsInterface && serviceName[0] == 'I' && Char.IsUpper(serviceName[1]))
                {
                    serviceName = serviceName.Substring(1);
                }

                serviceName = Char.ToLower(serviceName[0]) + serviceName.Substring(1);

                builder.Append(WrapKeyword("var"));
                builder.Append(
                    $" {Encode(serviceName)} = Activate&lt;{WrapType(Encode(job.Type.ToGenericTypeString()))}&gt;();");

                builder.AppendLine();
            }
            
            if (job.Method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
            {
                builder.Append($"{WrapKeyword("await")} ");
            }

            builder.Append(!job.Method.IsStatic ? Encode(serviceName) : WrapType(Encode(job.Type.ToGenericTypeString())));

            builder.Append(".");
            builder.Append(Encode(job.Method.Name));

            if (job.Method.IsGenericMethod)
            {
                var genericArgumentTypes = job.Method.GetGenericArguments()
                    .Select(x => WrapType(x.Name))
                    .ToArray();

                builder.Append($"&lt;{String.Join(", ", genericArgumentTypes)}&gt;");
            }

            builder.Append("(");

            var parameters = job.Method.GetParameters();
            var renderedArguments = new List<string>(parameters.Length);
            var renderedArgumentsTotalLength = 0;

            const int splitStringMinLength = 100;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

#pragma warning disable 618
                if (i < job.Arguments.Length)
                {
                    var argument = job.Arguments[i]; // TODO: check bounds
#pragma warning restore 618
                    string renderedArgument;

                    var enumerableArgument = GetIEnumerableGenericArgument(parameter.ParameterType);

                    object argumentValue;
                    bool isJson = true;

                    try
                    {
                        argumentValue = JobHelper.FromJson(argument, parameter.ParameterType);
                    }
                    catch (Exception)
                    {
                        // If argument value is not encoded as JSON (an old
                        // way using TypeConverter), we should display it as is.
                        argumentValue = argument;
                        isJson = false;
                    }

                    if (enumerableArgument == null)
                    {
                        var argumentRenderer = ArgumentRenderer.GetRenderer(parameter.ParameterType);
                        renderedArgument = argumentRenderer.Render(isJson, argumentValue?.ToString(), argument);
                    }
                    else
                    {
                        var renderedItems = new List<string>();

                        // ReSharper disable once LoopCanBeConvertedToQuery
                        foreach (var item in (IEnumerable)argumentValue)
                        {
                            var argumentRenderer = ArgumentRenderer.GetRenderer(enumerableArgument);
                            renderedItems.Add(argumentRenderer.Render(isJson, item.ToString(),
                                JobHelper.ToJson(item)));
                        }

                        // ReSharper disable once UseStringInterpolation
                        renderedArgument = String.Format(
                            "{0}{1} {{ {2} }}",
                            WrapKeyword("new"),
                            parameter.ParameterType.IsArray ? " []" : "",
                            String.Join(", ", renderedItems));
                    }

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
                // TODO: be aware of out of range
                var parameter = parameters[i];
                var tooltipPosition = "top";

                var renderedArgument = renderedArguments[i];
                if (renderedArgumentsTotalLength > splitStringMinLength)
                {
                    builder.AppendLine();
                    builder.Append("    ");

                    tooltipPosition = "left";
                }
                else if (i > 0)
                {
                    builder.Append(" ");
                }

                builder.Append($"<span title=\"{parameter.Name}\" data-placement=\"{tooltipPosition}\">");
                builder.Append(renderedArgument);
                builder.Append("</span>");

                if (i < renderedArguments.Count - 1)
                {
                    builder.Append(",");
                }
            }

            builder.Append(");");

            return new NonEscapedString(builder.ToString());
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

        private static string Span(string @class, string value)
        {
            return $"<span class=\"{@class}\">{value}</span>";
        }

        private static string Encode(string value)
        {
            return WebUtility.HtmlEncode(value);
        }

        private static Type GetIEnumerableGenericArgument(Type type)
        {
            if (type == typeof (string)) return null;

            return type.GetInterfaces()
                .Where(x => x.GetTypeInfo().IsGenericType
                            && x.GetTypeInfo().GetGenericTypeDefinition() == typeof (IEnumerable<>))
                .Select(x => x.GetTypeInfo().GetGenericArguments()[0])
                .FirstOrDefault();
        }

        public static string GetNameWithoutGenericArity(Type t)
        {
            string name = t.Name;
            int index = name.IndexOf('`');
            return index == -1 ? name : name.Substring(0, index);
        }

        private class ArgumentRenderer
        {
            private string _enclosingString;
            private Type _deserializationType;
            private Func<string, string> _valueRenderer;

            private ArgumentRenderer()
            {
                _enclosingString = "\"";
                _valueRenderer = value => value == null ? WrapKeyword("null") : WrapString(value);
            }

            public string Render(bool isJson, string deserializedValue, string rawValue)
            {
                var builder = new StringBuilder();
                if (_deserializationType != null)
                {
                    builder.Append(WrapIdentifier(
                        isJson ? "FromJson" : "Deserialize"));

                    builder.Append("&lt;")
                        .Append(WrapType(Encode(_deserializationType.Name)))
                        .Append(WrapIdentifier("&gt;"))
                        .Append("(");
                    
                    builder.Append(WrapString(Encode("\"" + rawValue.Replace("\"", "\\\"") + "\"")));
                }
                else
                {
                    if (deserializedValue != null)
                    {
                        builder.Append(_enclosingString);
                    }

                    builder.Append(_valueRenderer(Encode(deserializedValue)));

                    if (deserializedValue != null)
                    {
                        builder.Append(_enclosingString);
                    }
                }

                if (_deserializationType != null)
                {
                    builder.Append(")");
                }

                return builder.ToString();
            }

            public static ArgumentRenderer GetRenderer(Type type)
            {
                if (type.GetTypeInfo().IsEnum)
                {
                    return new ArgumentRenderer
                    {
                        _enclosingString = String.Empty,
                        _valueRenderer = value => $"{WrapType(type.Name)}.{value}"
                    };
                }

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
                        _valueRenderer = value => WrapKeyword(value.ToLowerInvariant()),
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
                        _enclosingString = "\""
                    };
                }

                if (type == typeof(TimeSpan) || type == typeof(DateTime))
                {
                    return new ArgumentRenderer
                    {
                        _enclosingString = String.Empty,
                        _valueRenderer = value => $"{WrapType(type.Name)}.Parse({WrapString($"\"{value}\"")})"
                    };
                }

                if (type == typeof (CancellationToken))
                {
                    return new ArgumentRenderer
                    {
                        _enclosingString = String.Empty,
                        _valueRenderer = value => $"{WrapType(nameof(CancellationToken))}.None"
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
                return type.GetTypeInfo().IsGenericType && type.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>);
            }
        }
    }
}
