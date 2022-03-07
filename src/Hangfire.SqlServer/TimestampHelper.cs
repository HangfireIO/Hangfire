// This file is part of Hangfire. Copyright © 2021 Sergey Odinokov.
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

using System;

namespace Hangfire.SqlServer
{
    internal static class TimestampHelper
    {
        public static long GetTimestamp()
        {
#if NETCOREAPP3_0
            return Environment.TickCount64;
#else
            return Environment.TickCount;
#endif
        }

        public static TimeSpan Elapsed(long timestamp)
        {
            var now = GetTimestamp();
            return Elapsed(now, timestamp);
        }

        public static TimeSpan Elapsed(long now, long timestamp)
        {
#if NETCOREAPP3_0
            return TimeSpan.FromMilliseconds(now - timestamp);
#else
            return TimeSpan.FromMilliseconds(unchecked((int)now - (int)timestamp));
#endif
        }
    }
}