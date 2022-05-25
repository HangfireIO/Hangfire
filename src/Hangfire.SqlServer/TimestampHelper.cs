// This file is part of Hangfire. Copyright © 2021 Hangfire OÜ.
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