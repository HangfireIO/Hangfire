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
Please use 'SetArgumentsSerializerSettings', 'SerializeArgument', 'DeserializeArgument' instead 
to serialize/deserialize arguments.
Please use 'SetParametersSerializerSettings', 'SerializeParameter', 'DeserializeParameter' instead 
to serialize/deserialize job parameters.
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
            return value != null
                ? JsonConvert.SerializeObject(value, _coreSerializerSettings)
                : null;
        }

        public static T FromJson<T>(string value)
        {
            return value != null
                ? JsonConvert.DeserializeObject<T>(value, _coreSerializerSettings)
                : default(T);
        }

        public static object FromJson(string value, [NotNull] Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return value != null
                ? JsonConvert.DeserializeObject(value, type, _coreSerializerSettings)
                : null;
        }

        public static string SerializeArgument(object value)
        {
            return value != null
               ? JsonConvert.SerializeObject(value, _arugumentsSerializerSettings)
               : null;
        }

        public static T DeserializeArgument<T>(string value)
        {
            return value != null
                ? JsonConvert.DeserializeObject<T>(value, _arugumentsSerializerSettings)
                : default(T);
        }

        public static object DeserializeArgument(string value, [NotNull] Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return value != null
                ? JsonConvert.DeserializeObject(value, type, _arugumentsSerializerSettings)
                : null;
        }

        public static T DeserializeParameter<T>(string value)
        {
            return value != null
                ? JsonConvert.DeserializeObject<T>(value, _parametersSerializerSettings)
                : default(T);
        }

        public static string SerializeParameter(object value)
        {
            return value != null
               ? JsonConvert.SerializeObject(value, _parametersSerializerSettings)
               : null;
        }

        public static object DeserializeParameter(string value, [NotNull] Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return value != null
                ? JsonConvert.DeserializeObject(value, type, _parametersSerializerSettings)
                : null;
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
