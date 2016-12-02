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
using System.Globalization;
using Hangfire.Annotations;
using Newtonsoft.Json;

namespace Hangfire.Common
{
    public static class JobHelper
    {
        [Obsolete("Please use `SerializationHelper.SetUserSerializationSettings` instead. Will be removed in 2.0.0")]
        public static void SetSerializerSettings(JsonSerializerSettings setting)
        {
            SerializationHelper.SetUserSerializerSettings(setting);
        }

        [Obsolete("Please use `SerializationHelper.Serialize` instead. Will be removed in 2.0.0")]
        public static string ToJson(object value)
        {
            return SerializationHelper.Serialize(value, SerializationOption.User);
        }

        [Obsolete("Please use `SerializationHelper.Deserialize` instead. Will be removed in 2.0.0")]
        public static T FromJson<T>(string value)
        {
            return SerializationHelper.Deserialize<T>(value, SerializationOption.User);
        }

        [Obsolete("Please use `SerializationHelper.Deserialize` instead. Will be removed in 2.0.0")]
        public static object FromJson(string value, [NotNull] Type type)
        {
            return SerializationHelper.Deserialize(value, type, SerializationOption.User);
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long) elapsedTime.TotalSeconds;
        }

        public static DateTime FromTimestamp(long value)
        {
            return Epoch.AddSeconds(value);
        }

        public static string SerializeDateTime(DateTime value)
        {
            return value.ToString("o", CultureInfo.InvariantCulture);
        }

        public static DateTime DeserializeDateTime(string value)
        {
            long timestamp;
            if (long.TryParse(value, out timestamp))
            {
                return FromTimestamp(timestamp);
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
