// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
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
        Internal,

        /// <summary>
        /// For internal data using isolated settings with types information (<see cref="TypeNameHandling.Objects"/> setting) 
        /// that can't be changed from user code.
        /// </summary>
        TypedInternal,

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
        private static readonly Lazy<JsonSerializerSettings> InternalSerializerSettings =
            new Lazy<JsonSerializerSettings>(GetInternalSettings, LazyThreadSafetyMode.PublicationOnly);

        private static JsonSerializerSettings _userSerializerSettings;

        /// <summary>
        /// Serializes data with <see cref="SerializationOption.Internal"/> option.
        /// Use this method to serialize internal data. Using isolated settings that can't be changed from user code.
        /// </summary>
        public static string Serialize<T>([CanBeNull] T value)
        {
            return Serialize(value, SerializationOption.Internal);
        }

        /// <summary>
        /// Serializes data with specified option. 
        /// Use <see cref="SerializationOption.Internal"/> option to serialize internal data.
        /// Use <see cref="SerializationOption.TypedInternal"/> option if you need to store type information.
        /// Use <see cref="SerializationOption.User"/> option to serialize user data like arguments and parameters,
        /// configurable via <see cref="SetUserSerializerSettings"/>.
        /// </summary>
        public static string Serialize<T>([CanBeNull] T value, SerializationOption option)
        {
            return Serialize(value, typeof(T), option);
        }

        /// <summary>
        /// Serializes data with specified option. 
        /// Use <see cref="SerializationOption.Internal"/> option to serialize internal data.
        /// Use <see cref="SerializationOption.TypedInternal"/> option if you need to store type information.
        /// Use <see cref="SerializationOption.User"/> option to serialize user data like arguments and parameters,
        /// configurable via <see cref="SetUserSerializerSettings"/>.
        /// </summary>
        public static string Serialize([CanBeNull] object value, [CanBeNull] Type type, SerializationOption option)
        {
            if (value == null) return null;

            if (GlobalConfiguration.HasCompatibilityLevel(CompatibilityLevel.Version_170))
            {
                var serializerSettings = GetSerializerSettings(option);

                if (option == SerializationOption.User)
                {
                    var formatting = serializerSettings?.Formatting ?? Formatting.None;
                    return JsonConvert.SerializeObject(value, type, formatting, serializerSettings);
                }

                // For internal purposes we should ensure that JsonConvert.DefaultSettings don't affect
                // the serialization process, and the only way is to create a custom serializer.
                using (var stringWriter = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture))
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                {
                    var serializer = JsonSerializer.Create(serializerSettings);
                    serializer.Serialize(jsonWriter, value, type);

                    return stringWriter.ToString();
                }
            }
            else
            {
                // Previously almost all the data was serialized with the user settings, except
                // when we explicitly needed to persist the type information. In the latter case
                // custom settings passed to serializer, identical to TypedInternal.
                var serializerSettings = option == SerializationOption.TypedInternal
                    ? GetLegacyTypedSerializerSettings()
                    : GetUserSerializerSettings();

                // JsonConvert is used here, because previously global default settings affected
                // the serialization process.
                var formatting = serializerSettings?.Formatting ?? Formatting.None;
                return JsonConvert.SerializeObject(value, type, formatting, serializerSettings);
            }
        }

        /// <summary>
        /// Deserializes data with <see cref="SerializationOption.Internal"/> option.
        /// Use this method to deserialize internal data. Using isolated settings that can't be changed from user code.
        /// </summary>
        public static object Deserialize([CanBeNull] string value, [NotNull] Type type)
        {
            return Deserialize(value, type, SerializationOption.Internal);
        }

        /// <summary>
        /// Deserializes data with specified option. 
        /// Use <see cref="SerializationOption.Internal"/> to deserialize internal data.
        /// Use <see cref="SerializationOption.TypedInternal"/> if deserializable internal data has type names information.
        /// Use <see cref="SerializationOption.User"/> to deserialize user data like arguments and parameters, 
        /// configurable via <see cref="SetUserSerializerSettings"/>.
        /// </summary>
        public static object Deserialize([CanBeNull] string value, [NotNull] Type type, SerializationOption option)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (value == null) return null;

            Exception exception = null;

            if (option != SerializationOption.User)
            {
                var serializerSettings = GetSerializerSettings(option);

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
                catch (Exception ex) when (ex.IsCatchableExceptionType())
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
                return JsonConvert.DeserializeObject(value, type, GetSerializerSettings(SerializationOption.User));
            }
            catch (Exception ex) when (exception != null && ex.IsCatchableExceptionType())
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw;
            }
        }

        /// <summary>
        /// Deserializes data with <see cref="SerializationOption.Internal"/> option.
        /// Use this method to deserialize internal data. Using isolated settings that can't be changed from user code.
        /// </summary>
        public static T Deserialize<T>([CanBeNull] string value)
        {
            if (value == null) return default(T);
            return Deserialize<T>(value, SerializationOption.Internal);
        }

        /// <summary>
        /// Deserializes data with specified option. 
        /// Use <see cref="SerializationOption.Internal"/> to deserialize internal data.
        /// Use <see cref="SerializationOption.TypedInternal"/> if deserializable internal data has type names information.
        /// Use <see cref="SerializationOption.User"/> to deserialize user data like arguments and parameters, 
        /// configurable via <see cref="SetUserSerializerSettings"/>.
        /// </summary>
        public static T Deserialize<T>([CanBeNull] string value, SerializationOption option)
        {
            if (value == null) return default(T);
            return (T) Deserialize(value, typeof(T), option);
        }

        internal static JsonSerializerSettings GetInternalSettings()
        {
            var serializerSettings = new JsonSerializerSettings();

            SetSimpleTypeNameAssemblyFormat(serializerSettings);

            serializerSettings.TypeNameHandling = TypeNameHandling.Auto;
            serializerSettings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
            serializerSettings.NullValueHandling = NullValueHandling.Ignore;
            serializerSettings.CheckAdditionalContent = true; // Default option in JsonConvert.Deserialize method
#if NETSTANDARD2_0
            serializerSettings.SerializationBinder = new TypeHelperSerializationBinder();
#else
            serializerSettings.Binder = new TypeHelperSerializationBinder();
#endif

            return serializerSettings;
        }

        internal static void SetUserSerializerSettings([CanBeNull] JsonSerializerSettings settings)
        {
            Volatile.Write(ref _userSerializerSettings, settings);
        }

        private static JsonSerializerSettings GetLegacyTypedSerializerSettings()
        {
            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.TypeNameHandling = TypeNameHandling.Objects;

            SetSimpleTypeNameAssemblyFormat(serializerSettings);

            return serializerSettings;
        }

        private static void SetSimpleTypeNameAssemblyFormat(JsonSerializerSettings serializerSettings)
        {
            // Setting TypeNameAssemblyFormatHandling to Simple. Using reflection, because latest versions
            // of Newtonsoft.Json contain breaking changes.
            var typeNameAssemblyFormatHandling =
                typeof(JsonSerializerSettings).GetRuntimeProperty("TypeNameAssemblyFormatHandling");
            var typeNameAssemblyFormat = typeof(JsonSerializerSettings).GetRuntimeProperty("TypeNameAssemblyFormat");

            var property = typeNameAssemblyFormatHandling ?? typeNameAssemblyFormat;
            property.SetValue(serializerSettings, Enum.Parse(property.PropertyType, "Simple"));
        }

        private static JsonSerializerSettings GetSerializerSettings(SerializationOption serializationOption)
        {
            switch (serializationOption)
            {
                case SerializationOption.Internal:
                case SerializationOption.TypedInternal: return InternalSerializerSettings.Value;
                case SerializationOption.User: return GetUserSerializerSettings();
                default: throw new ArgumentOutOfRangeException(nameof(serializationOption), serializationOption, null);
            }
        }

        private static JsonSerializerSettings GetUserSerializerSettings()
        {
            return Volatile.Read(ref _userSerializerSettings);
        }
    }
}
