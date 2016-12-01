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
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization.Formatters;
using Hangfire.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hangfire.Common
{
    public static class JobHelper
    {
        private const TypeNameHandling DefaulTypeNameHandling = TypeNameHandling.None;

        private static readonly Func<TypeNameHandling, JsonSerializerSettings> CoreSerializerSettingsFactory =
            typeNameHandling => new JsonSerializerSettings
            {
                TypeNameHandling = typeNameHandling,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,

                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateFormatString = @"yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK",

                Formatting = Formatting.None,
                CheckAdditionalContent = false,

                ConstructorHandling = ConstructorHandling.Default,
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include,
                ObjectCreationHandling = ObjectCreationHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                Culture = CultureInfo.InvariantCulture,
                Binder = new DefaultSerializationBinder()
            };

        private static readonly JsonSerializerSettings CoreSerializerSettings = CoreSerializerSettingsFactory(DefaulTypeNameHandling);

        private static JsonSerializerSettings _argumentsAndParametersSerializerSettings;

        public static void SetSerializerSettings(JsonSerializerSettings setting)
        {
            _argumentsAndParametersSerializerSettings = setting;
        }

        public static string ToJson(object value)
        {
            return value != null
                ? JsonConvert.SerializeObject(value, _argumentsAndParametersSerializerSettings)
                : null;
        }

        public static T FromJson<T>(string value)
        {
            return value != null
                ? JsonConvert.DeserializeObject<T>(value, _argumentsAndParametersSerializerSettings)
                : default(T);
        }

        public static object FromJson(string value, [NotNull] Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return value != null
                ? JsonConvert.DeserializeObject(value, type, _argumentsAndParametersSerializerSettings)
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

        internal static string Serialize(object value, TypeNameHandling typeNameHandling = DefaulTypeNameHandling)
        {
            if (value == null) return null;

            var serializerSettings = GetSerializerSettings(typeNameHandling);

            return JsonConvert.SerializeObject(value, serializerSettings);
        }

        internal static T Deserialize<T>(string value, TypeNameHandling typeNameHandling = DefaulTypeNameHandling)
        {
            if (value == null) return default(T);

            var serializerSettings = GetSerializerSettings(typeNameHandling);
            
            try
            {
                return JsonConvert.DeserializeObject<T>(value, serializerSettings);
            }
            catch (Exception jsonException)
            {
                try
                {
                    return FromJson<T>(value);
                }
                catch (Exception)
                {
                    ExceptionDispatchInfo.Capture(jsonException).Throw();
                    throw;
                }
            }
        }

        private static JsonSerializerSettings GetSerializerSettings(TypeNameHandling typeNameHandling)
        {
            return typeNameHandling == DefaulTypeNameHandling
                ? CoreSerializerSettings
                : CoreSerializerSettingsFactory(typeNameHandling);
        }
    }
}
