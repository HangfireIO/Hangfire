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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Hangfire.Common;
using Hangfire.Dashboard.Resources;
using Hangfire.Processing;
using Hangfire.Storage;

namespace Hangfire.Dashboard
{
    internal static class JobMethodCallRenderer
    {
        private static readonly int MaxArgumentToRenderSize = 4096;

        public static NonEscapedString Render(Job job, InvocationData invocationData)
        {
            if (job == null &&
                invocationData is null)
            {
                return new NonEscapedString(RenderCannotFindTargetMethod());
            }
            var builder = new StringBuilder();
            IReadOnlyList<string> arguments;
            if (job is null)
            {
                builder.AppendLine(WrapComment(RenderCannotFindTargetMethod()));
                RenderNamespace(job, invocationData, builder);
                builder.Append(WrapType(Encode(invocationData.TypeName)));
                builder.Append(TypeHelper.MemberSeparator);
                builder.Append(Encode(invocationData.Method));
                arguments = invocationData.ArgumentList;
            }
            else
            {
                RenderNamespace(job, invocationData, builder);
                string serviceName = RenderActivation(job, builder);

                if (job.Method.GetCustomAttribute<AsyncStateMachineAttribute>() != null ||
                    job.Method.ReturnType.IsTaskLike(out _))
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

                    builder.Append($"&lt;{String.Join(TypeHelper.GenericArgumentSeparator, genericArgumentTypes)}&gt;");
                }

#pragma warning disable 618
                arguments = job.Arguments;
#pragma warning restore 618
            }
            builder.Append("(");

            var parameters = job?.Method.GetParameters();
            int parameterCount = parameters?.Length ?? arguments.Count;
            var renderedArguments = new List<string>(parameterCount);
            var renderedArgumentsTotalLength = 0;

            for (var i = 0; i < parameterCount; i++)
            {
                var parameter = parameters is null ?
                    null :
                    parameters[i];
                var parameterType = parameter?.ParameterType;
                if (i < arguments.Count)
                {
                    var argument = arguments[i];

                    if (argument != null && argument.Length > MaxArgumentToRenderSize)
                    {
                        renderedArguments.Add(Encode("<VALUE IS TOO BIG>"));
                        continue;
                    }

                    string renderedArgument;

                    var enumerableArgument = GetIEnumerableGenericArgument(parameterType);

                    object argumentValue;
                    bool isJson;
                    if (parameterType is null)
                    {
                        argumentValue = argument;
                        isJson = false;
                    }
                    else
                    {
                        try
                        {
                            argumentValue = SerializationHelper.Deserialize(argument, parameterType, SerializationOption.User);
                            isJson = true;
                        }
                        catch (Exception)
                        {
                            // If argument value is not encoded as JSON (an old
                            // way using TypeConverter), we should display it as is.
                            argumentValue = argument;
                            isJson = false;
                        }
                    }

                    if (enumerableArgument == null || argumentValue == null)
                    {
                        var argumentRenderer = ArgumentRenderer.GetRenderer(parameterType);
                        renderedArgument = argumentRenderer.Render(isJson, argumentValue?.ToString(), argument);
                    }
                    else
                    {
                        var renderedItems = new List<string>();

                        // ReSharper disable once LoopCanBeConvertedToQuery
                        foreach (var item in (IEnumerable)argumentValue)
                        {
                            var argumentRenderer = ArgumentRenderer.GetRenderer(enumerableArgument);
                            renderedItems.Add(argumentRenderer.Render(isJson, item?.ToString(),
                                SerializationHelper.Serialize(item, SerializationOption.User)));
                        }

                        // ReSharper disable once UseStringInterpolation
                        renderedArgument = String.Format(
                            "{0}{1} {{ {2} }}",
                            WrapKeyword("new"),
                            parameterType.IsArray ? " []" : "",
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

            const int splitStringMinLength = 100;

            for (int i = 0; i < renderedArguments.Count; i++)
            {
                // TODO: be aware of out of range
                var parameter = parameters is null ?
                    null :
                    parameters[i];
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

                if (parameter != null)
                    builder.Append($"<span title=\"{parameter.Name}\" data-placement=\"{tooltipPosition}\">");
                builder.Append(renderedArgument);
                if (parameter != null)
                    builder.Append("</span>");

                if (i < renderedArguments.Count - 1)
                {
                    builder.Append(",");
                }
            }

            builder.Append(");");

            return new NonEscapedString(builder.ToString());
        }

        private static string RenderCannotFindTargetMethod() => $"<em>{Encode(Strings.Common_CannotFindTargetMethod)}</em>";

        private static void RenderNamespace(Job job, InvocationData invocationData, StringBuilder builder)
        {
            string @namespace = job?.Type.Namespace ?? invocationData.TypeNamespace;
            if (@namespace is null)
                return;
            builder.Append(WrapKeyword("using"));
            builder.Append(" ");
            builder.Append(Encode(@namespace));
            builder.Append(";");
            builder.AppendLine();
            builder.AppendLine();
        }

        private static string RenderActivation(Job job, StringBuilder builder)
        {
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
            return serviceName;
        }

        private static string Encode(object p) => throw new NotImplementedException();

        private static string WrapIdentifier(string value)
        {
            return value;
        }

        private static string WrapKeyword(string value)
        {
            return Span("keyword", value);
        }

        private static string WrapComment(string value) => Span("comment", "// " + value);

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
            if (type is null ||
                type == typeof(string)
                )
            {
                return null;
            }
            return type.GetTypeInfo().ImplementedInterfaces
                .Where(x => x.GetTypeInfo().IsGenericType
                            && x.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(x => x.GetTypeInfo().GetAllGenericArguments()[0])
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

                if (rawValue == null)
                {
                    return WrapKeyword("null");
                }

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
                if (type is null)
                {
                    return new ArgumentRenderer
                    {
                        _enclosingString = string.Empty
                    };
                }
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

                if (type == typeof(CancellationToken))
                {
                    return new ArgumentRenderer
                    {
                        _enclosingString = String.Empty,
                        _valueRenderer = value => $"{WrapType(nameof(CancellationToken))}.None"
                    };
                }

                return new ArgumentRenderer
                {
                    _deserializationType = type
                };
            }

            private static bool IsNumericType(Type type)
            {
                if (type == null) return false;

                switch (type.GetTypeCode())
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

    internal enum TypeCode
    {
        Empty = 0,          // Null reference
        Object = 1,         // Instance that isn't a value
        DBNull = 2,         // Database null value
        Boolean = 3,        // Boolean
        Char = 4,           // Unicode character
        SByte = 5,          // Signed 8-bit integer
        Byte = 6,           // Unsigned 8-bit integer
        Int16 = 7,          // Signed 16-bit integer
        UInt16 = 8,         // Unsigned 16-bit integer
        Int32 = 9,          // Signed 32-bit integer
        UInt32 = 10,        // Unsigned 32-bit integer
        Int64 = 11,         // Signed 64-bit integer
        UInt64 = 12,        // Unsigned 64-bit integer
        Single = 13,        // IEEE 32-bit float
        Double = 14,        // IEEE 64-bit double
        Decimal = 15,       // Decimal
        DateTime = 16,      // DateTime
        String = 18,        // Unicode character string
    }

    internal static class TypeExtensionMethods
    {
        public static TypeCode GetTypeCode(this Type type)
        {
            if (type == null)
            {
                return TypeCode.Empty;
            }
            else if (type == typeof(Boolean))
            {
                return TypeCode.Boolean;
            }
            else if (type == typeof(Char))
            {
                return TypeCode.Char;
            }
            else if (type == typeof(SByte))
            {
                return TypeCode.SByte;
            }
            else if (type == typeof(Byte))
            {
                return TypeCode.Byte;
            }
            else if (type == typeof(Int16))
            {
                return TypeCode.Int16;
            }
            else if (type == typeof(UInt16))
            {
                return TypeCode.UInt16;
            }
            else if (type == typeof(Int32))
            {
                return TypeCode.Int32;
            }
            else if (type == typeof(UInt32))
            {
                return TypeCode.UInt32;
            }
            else if (type == typeof(Int64))
            {
                return TypeCode.Int64;
            }
            else if (type == typeof(UInt64))
            {
                return TypeCode.UInt64;
            }
            else if (type == typeof(Single))
            {
                return TypeCode.Single;
            }
            else if (type == typeof(Double))
            {
                return TypeCode.Double;
            }
            else if (type == typeof(Decimal))
            {
                return TypeCode.Decimal;
            }
            else if (type == typeof(DateTime))
            {
                return TypeCode.DateTime;
            }
            else if (type == typeof(String))
            {
                return TypeCode.String;
            }
            else
            {
                return TypeCode.Object;
            }
        }
    }
}
