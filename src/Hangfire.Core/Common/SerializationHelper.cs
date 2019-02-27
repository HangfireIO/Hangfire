// This file is part of Hangfire.
// Copyright © 2019 Sergey Odinokov.
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
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Hangfire.Annotations;
using Newtonsoft.Json;

namespace Hangfire.Common
{
    public enum SerializationOption
    {
        /// <summary>
        /// For internal data using isolated settings that can't be changed from user code.
        /// </summary>
        Default,

        /// <summary>
        /// For internal data using isolated settings with types information (<see cref="TypeNameHandling.Objects"/> setting) 
        /// that can't be changed from user code.
        /// </summary>
        DefaultWithTypes,

        /// <summary>
        /// For user data like arguments and parameters, configurable via <see cref="SerializationHelper.SetUserSerializerSettings"/>.
        /// </summary>
        User
    }

    /// <summary>
    /// Provides methods to serialize/deserialize data with Hangfire default settings. 
    /// Isolates internal serialization process from user interference including `JsonConvert.DefaultSettings` modification.
    /// </summary>
    public static class SerializationHelper
    {
        private static readonly Lazy<JsonSerializerSettings> DefaultSerializerSettings =
            new Lazy<JsonSerializerSettings>(() => GetProtectedSettings(TypeNameHandling.None), LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<JsonSerializerSettings> DefaultSerializerSettingsWithTypes =
            new Lazy<JsonSerializerSettings>(() => GetProtectedSettings(TypeNameHandling.Objects), LazyThreadSafetyMode.PublicationOnly);

        private static JsonSerializerSettings _userSerializerSettings;

        /// <summary>
        /// Serializes data with <see cref="SerializationOption.Default"/> option.
        /// Use this method to serialize internal data. Using isolated settings that can't be changed from user code.
        /// </summary>
        public static string Serialize(object value)
        {
            return Serialize(value, SerializationOption.Default);
        }

        /// <summary>
        /// Serializes data with specified option. 
        /// Use <see cref="SerializationOption.Default"/> option to serialize internal data.
        /// Use <see cref="SerializationOption.DefaultWithTypes"/> option if you need to store type information.
        /// Use <see cref="SerializationOption.User"/> option to serialize user data like arguments and parameters,
        /// configurable via <see cref="SetUserSerializerSettings"/>.
        /// </summary>
        public static string Serialize(object value, SerializationOption option)
        {
            if (value == null) return null;

            var serializerSettings = GetSerializerSettings(option);

            if (option == SerializationOption.User)
            {
                return JsonConvert.SerializeObject(value, serializerSettings);
            }

            // For internal purposes we should ensure that JsonConvert.DefaultSettings don't affect
            // the serialization process, and the only way is to create a custom serializer.
            using (var stringWriter = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture))
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                var serializer = JsonSerializer.Create(serializerSettings);
                serializer.Serialize(jsonWriter, value, null);

                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Deserializes data with <see cref="SerializationOption.Default"/> option.
        /// Use this method to deserialize internal data. Using isolated settings that can't be changed from user code.
        /// </summary>
        public static object Deserialize(string value, [NotNull] Type type)
        {
            return Deserialize(value, type, SerializationOption.Default);
        }

        /// <summary>
        /// Deserializes data with specified option. 
        /// Use <see cref="SerializationOption.Default"/> to deserialize internal data.
        /// Use <see cref="SerializationOption.DefaultWithTypes"/> if deserializable internal data has type names information.
        /// Use <see cref="SerializationOption.User"/> to deserialize user data like arguments and parameters, 
        /// configurable via <see cref="SetUserSerializerSettings"/>.
        /// </summary>
        public static object Deserialize(string value, [NotNull] Type type, SerializationOption option)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (value == null) return null;

            var serializerSettings = GetSerializerSettings(option);
            Exception exception = null;

            if (option != SerializationOption.User)
            {
                try
                {
                    // For internal purposes we should ensure that JsonConvert.DefaultSettings don't affect
                    // the deserialization process, and the only way is to create a custom serializer.
                    using (var stringReader = new StringReader(value))
                    using (var jsonReader = new JsonTextReader(stringReader))
                    {
                        var serializer = JsonSerializer.Create(serializerSettings);
                        return serializer.Deserialize(jsonReader, type);
                    }
                }
                catch (Exception ex)
                {
                    // If there was an exception, we should try to deserialize the value using user-based
                    // settings, because prior to 1.7.0 they were used for almost everything. So we are saving
                    // the exception to re-throw it if even serializer based on user settings couldn't handle
                    // our value. In that case an original exception should be thrown as it is the reason.
                    exception = ex;
                }
            }

            try
            {
                return JsonConvert.DeserializeObject(value, type, serializerSettings);
            }
            catch (Exception) when (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw;
            }
        }

        /// <summary>
        /// Deserializes data with <see cref="SerializationOption.Default"/> option.
        /// Use this method to deserialize internal data. Using isolated settings that can't be changed from user code.
        /// </summary>
        public static T Deserialize<T>(string value)
        {
            if (value == null) return default(T);
            return Deserialize<T>(value, SerializationOption.Default);
        }

        /// <summary>
        /// Deserializes data with specified option. 
        /// Use <see cref="SerializationOption.Default"/> to deserialize internal data.
        /// Use <see cref="SerializationOption.DefaultWithTypes"/> if deserializable internal data has type names information.
        /// Use <see cref="SerializationOption.User"/> to deserialize user data like arguments and parameters, 
        /// configurable via <see cref="SetUserSerializerSettings"/>.
        /// </summary>
        public static T Deserialize<T>(string value, SerializationOption option)
        {
            if (value == null) return default(T);
            return (T) Deserialize(value, typeof(T), option);
        }

        internal static JsonSerializerSettings GetProtectedSettings(TypeNameHandling typeNameHandling)
        {
            var serializerSettings = new JsonSerializerSettings();

            // Setting TypeNameAssemblyFormatHandling to Simple. Using reflection, because latest versions
            // of Newtonsoft.Json contain breaking changes.
            var typeNameAssemblyFormatHandling = typeof(JsonSerializerSettings).GetRuntimeProperty("TypeNameAssemblyFormatHandling");
            var typeNameAssemblyFormat = typeof(JsonSerializerSettings).GetRuntimeProperty("TypeNameAssemblyFormat");

            var property = typeNameAssemblyFormatHandling ?? typeNameAssemblyFormat;
            property.SetValue(serializerSettings, Enum.Parse(property.PropertyType, "Simple"));

            serializerSettings.TypeNameHandling = typeNameHandling;
            serializerSettings.CheckAdditionalContent = true; // Default option in JsonConvert.Deserialize method

            return serializerSettings;
        }

        internal static void SetUserSerializerSettings(JsonSerializerSettings settings)
        {
            Volatile.Write(ref _userSerializerSettings, settings);
        }

        private static JsonSerializerSettings GetSerializerSettings(SerializationOption serializationOption)
        {
            switch (serializationOption)
            {
                case SerializationOption.Default: return DefaultSerializerSettings.Value;
                case SerializationOption.DefaultWithTypes: return DefaultSerializerSettingsWithTypes.Value;
                case SerializationOption.User: return Volatile.Read(ref _userSerializerSettings);
                default: throw new ArgumentOutOfRangeException(nameof(serializationOption), serializationOption, null);
            }
        }
    }
}
