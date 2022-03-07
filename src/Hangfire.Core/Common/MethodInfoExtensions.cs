// This file is part of Hangfire. Copyright © 2017 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System.Linq;
using System.Reflection;

namespace Hangfire.Common
{
    internal static class MethodInfoExtensions
    {
        public static string GetNormalizedName(this MethodInfo methodInfo)
        {
            // Method names containing '.' are considered explicitly implemented interface methods
            // https://stackoverflow.com/a/17854048/1398672
            return methodInfo.Name.Contains(".") && methodInfo.IsFinal && methodInfo.IsPrivate
                ? methodInfo.Name.Split('.').Last()
                : methodInfo.Name;
        }
    }
}