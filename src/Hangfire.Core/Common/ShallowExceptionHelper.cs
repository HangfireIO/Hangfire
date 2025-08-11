// This file is part of Hangfire. Copyright © 2017 Hangfire OÜ.
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
using System.IO;
using System.Text;
using Hangfire.Annotations;

namespace Hangfire.Common
{
    internal static class ShallowExceptionHelper
    {
        private static readonly object DataKey = "OriginalStackTrace";

        public static void PreserveOriginalStackTrace(this Exception exception)
        {
            if (exception != null && !exception.Data.Contains(DataKey))
            {
                exception.Data.Add(DataKey, GetStackTrace(exception, includeFileInfo: false));
            }
        }

        public static string ToStringWithOriginalStackTrace([NotNull] this Exception exception, int? numLines, bool includeFileInfo)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            return GetFirstLines(ToStringHelper(exception, false, includeFileInfo), numLines);
        }

        private static string ToStringHelper(Exception exception, bool isInner, bool includeFileInfo)
        {
            var sb = new StringBuilder();
            sb.Append(exception.GetType().FullName);
            sb.Append(": ");
            sb.Append(exception.Message);

            if (exception.InnerException != null)
            {
                sb.Append(" ---> ");
                sb.Append(ToStringHelper(exception.InnerException, true, includeFileInfo));
            }
            else sb.Append('\n');

            var stackTrace = exception.Data.Contains(DataKey) ? (string)exception.Data[DataKey] : GetStackTrace(exception, includeFileInfo);
            if (!String.IsNullOrWhiteSpace(stackTrace))
            {
                sb.Append(stackTrace);
                sb.Append('\n');
            }

            if (isInner) sb.Append("   --- End of inner exception stack trace ---\n");

            return sb.ToString();
        }

        private static string GetFirstLines(string text, int? numLines)
        {
            if (text == null) return null;
            if (!numLines.HasValue || numLines.Value < 0) return text;

            using (var reader = new StringReader(text))
            {
                var builder = new StringBuilder();

                while (numLines-- > 0)
                {
                    var line = reader.ReadLine();
                    if (line == null) break;

                    if (builder.Length > 0) builder.AppendLine();
                    builder.Append(line);
                }

                return builder.ToString();
            }
        }

        private static string GetStackTrace(Exception ex, bool includeFileInfo)
        {
#if NETSTANDARD1_3
            return ex.StackTrace;
#else
            return new System.Diagnostics.StackTrace(ex, includeFileInfo).ToString();
#endif
        }
    }
}