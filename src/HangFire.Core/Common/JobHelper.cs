// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using Newtonsoft.Json;

namespace HangFire.Common
{
    public static class JobHelper
    {
        public static string ToJson(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public static T FromJson<T>(string value)
        {
            return value != null 
                ? JsonConvert.DeserializeObject<T>(value)
                : default(T);
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime FromTimestamp(long value)
        {
            return Epoch.AddSeconds(value);
        }

        public static string ToStringTimestamp(DateTime value)
        {
            return ToTimestamp(value).ToString();
        }

        public static DateTime FromStringTimestamp(string value)
        {
            return FromTimestamp(long.Parse(value));
        }

        public static DateTime? FromNullableStringTimestamp(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            return FromStringTimestamp(value);
        }
    }
}
