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
        private static JsonSerializerSettings _coreSerializerSettings;
        private static JsonSerializerSettings _arugumentsSerializerSettings;
        private static JsonSerializerSettings _parametersSerializerSettings;

        [Obsolete(@"This method is here for compatibility reasons. 
Please use 'SetArgumentsSerializerSettings', 'SetParametersSerializerSettings' methods instead.
It will be impossible to affect 'ToJson', 'FromJson' methods behavior in version 2.0.0.
Will be removed in version 2.0.0.")]
        public static void SetSerializerSettings(JsonSerializerSettings settings)
        {
            _coreSerializerSettings = settings;
            _arugumentsSerializerSettings = settings;
            _parametersSerializerSettings = settings;
        }

        public static void SetArgumentsSerializerSettings(JsonSerializerSettings settings)
        {
            _arugumentsSerializerSettings = settings;
        }

        public static void SetParametersSerializerSettings(JsonSerializerSettings settings)
        {
            _parametersSerializerSettings = settings;
        }

        public static string ToJson(object value)
        {
            return Serialize(value, _coreSerializerSettings);
        }

        public static T FromJson<T>(string value)
        {
            return Deserialize<T>(value, _coreSerializerSettings);
        }

        public static object FromJson(string value, [NotNull] Type type)
        {
            return Deserialize(value, type, _coreSerializerSettings);
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

        internal static string SerializeArgument(object value)
        {
            return Serialize(value, _arugumentsSerializerSettings);
        }

        internal static object DeserializeArgument(string value, [NotNull] Type type)
        {
            return Deserialize(value, type, _arugumentsSerializerSettings);
        }

        internal static string SerializeParameter(object value)
        {
            return Serialize(value, _parametersSerializerSettings);
        }

        internal static T DeserializeParameter<T>(string value)
        {
            return Deserialize<T>(value, _parametersSerializerSettings);
        }

        private static string Serialize(object value, JsonSerializerSettings settings)
        {
            return value != null
               ? JsonConvert.SerializeObject(value, settings)
               : null;
        }

        private static object Deserialize(string value, [NotNull] Type type, JsonSerializerSettings settings)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return value != null
                ? JsonConvert.DeserializeObject(value, type, settings)
                : null;
        }

        private static T Deserialize<T>(string value, JsonSerializerSettings settings)
        {
            return value != null
                ? JsonConvert.DeserializeObject<T>(value, settings)
                : default(T);
        }
    }
}
