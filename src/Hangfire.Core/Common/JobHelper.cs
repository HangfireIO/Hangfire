// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Globalization;
using Hangfire.Annotations;
using Newtonsoft.Json;

namespace Hangfire.Common
{
    public static class JobHelper
    {
        [Obsolete("Please use `GlobalConfiguration.UseSerializerSettings` instead. Will be removed in 2.0.0")]
        public static void SetSerializerSettings(JsonSerializerSettings setting)
        {
            SerializationHelper.SetUserSerializerSettings(setting);
        }

        [Obsolete("Please use `SerializationHelper.Serialize` with appropriate serialization option instead. Will be removed in 2.0.0")]
        public static string ToJson(object value)
        {
            return SerializationHelper.Serialize(value, null, SerializationOption.User);
        }

        [Obsolete("Please use `SerializationHelper.Deserialize` with appropriate serialization option instead. Will be removed in 2.0.0")]
        public static T FromJson<T>(string value)
        {
            return SerializationHelper.Deserialize<T>(value, SerializationOption.User);
        }

        [Obsolete("Please use `SerializationHelper.Deserialize` with appropriate serialization option instead. Will be removed in 2.0.0")]
        public static object FromJson(string value, [NotNull] Type type)
        {
            return SerializationHelper.Deserialize(value, type, SerializationOption.User);
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime MillisecondTimestampBoundaryDate = new DateTime(1978, 1, 11, 21, 31, 40, 799, DateTimeKind.Utc);
        private static readonly long MillisecondTimestampBoundary = 253402300799L;

        public static long ToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime FromTimestamp(long value)
        {
            return Epoch.AddSeconds(value);
        }

        public static long ToMillisecondTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalMilliseconds;
        }

        public static DateTime FromMillisecondTimestamp(long value)
        {
            return Epoch.AddMilliseconds(value);
        }

        public static string SerializeDateTime(DateTime value)
        {
            if (value > MillisecondTimestampBoundaryDate && 
                value < DateTime.MaxValue &&
                GlobalConfiguration.HasCompatibilityLevel(CompatibilityLevel.Version_170))
            {
                return ToMillisecondTimestamp(value).ToString("D", CultureInfo.InvariantCulture);
            }

            return value.ToString("O", CultureInfo.InvariantCulture);
        }

        public static DateTime DeserializeDateTime(string value)
        {
            if (long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp))
            {
                return timestamp > MillisecondTimestampBoundary
                    ? FromMillisecondTimestamp(timestamp) 
                    : FromTimestamp(timestamp);
            }

            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        public static DateTime? DeserializeNullableDateTime(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            return DeserializeDateTime(value);
        }
    }
}
