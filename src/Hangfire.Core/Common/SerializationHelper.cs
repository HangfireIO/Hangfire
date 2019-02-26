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
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
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
        private static readonly PropertyInfo TypeNameAssemblyFormat = typeof(JsonSerializerSettings).GetRuntimeProperty(nameof(TypeNameAssemblyFormat));
        private static readonly PropertyInfo TypeNameAssemblyFormatHandling = typeof(JsonSerializerSettings).GetRuntimeProperty(nameof(TypeNameAssemblyFormatHandling));

        private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings();
        private static readonly JsonSerializerSettings DefaultSerializerSettingsWithTypes = new JsonSerializerSettings();

        private static JsonSerializerSettings _userSerializerSettings;

        static SerializationHelper()
        {
            ApplyDefaultSerializerSettings(DefaultSerializerSettings);
            ApplyDefaultSerializerSettings(DefaultSerializerSettingsWithTypes, TypeNameHandling.Objects);
        }

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

            return JsonConvert.SerializeObject(value, serializerSettings);
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

            try
            {
                return JsonConvert.DeserializeObject(value, type, serializerSettings);
            }
            catch (Exception outerException) when (option != SerializationOption.User)
            {
                try
                {
                    // It's here for backward compatibility. Earlier internal data serializer used user setting.
                    return Deserialize(value, type, SerializationOption.User);
                }
                catch (Exception)
                {
                    ExceptionDispatchInfo.Capture(outerException).Throw();
                    throw;
                }
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

        internal static void ApplyDefaultSerializerSettings(
            [NotNull] JsonSerializerSettings serializerSettings,
            TypeNameHandling typeNameHandling = TypeNameHandling.None)
        {
            if (serializerSettings == null) throw new ArgumentNullException(nameof(serializerSettings));

            // Setting TypeNameAssemblyFormatHandling to Simple. Using reflection, because latest versions
            // of Newtonsoft.Json contain breaking changes.
            var property = TypeNameAssemblyFormatHandling ?? TypeNameAssemblyFormat;
            property.SetValue(serializerSettings, Enum.Parse(property.PropertyType, "Simple"));

            //It's necessary to set default values explicitly in order to `JsonConvert.DefaultSettings` don't affect hangfire serialization.
            //See more http://www.newtonsoft.com/json/help/html/DefaultSettings.htm and 
            //https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/JsonConvert.cs#L63

            // TODO: ENSURE ALL OF THEM ARE SUPPORTED ON THE LATEST VERSION
            serializerSettings.TypeNameHandling = typeNameHandling;
            serializerSettings.NullValueHandling = NullValueHandling.Ignore;
            serializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
            serializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            serializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
            serializerSettings.DateParseHandling = DateParseHandling.DateTime;
            serializerSettings.FloatFormatHandling = FloatFormatHandling.DefaultValue;
            serializerSettings.FloatParseHandling = FloatParseHandling.Double;
            serializerSettings.StringEscapeHandling = StringEscapeHandling.Default;
            serializerSettings.DateFormatString = @"yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
            serializerSettings.Formatting = Formatting.None;
            serializerSettings.CheckAdditionalContent = false;
            serializerSettings.ConstructorHandling = ConstructorHandling.Default;
            serializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Error;
            serializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
            serializerSettings.ObjectCreationHandling = ObjectCreationHandling.Auto;
            serializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.None;
            serializerSettings.Culture = CultureInfo.InvariantCulture;
            serializerSettings.Context = new StreamingContext();

            var defaultJsonSerializer = new JsonSerializer();

            serializerSettings.ContractResolver = defaultJsonSerializer.ContractResolver;
#pragma warning disable 618
            serializerSettings.ReferenceResolver = defaultJsonSerializer.ReferenceResolver;
#pragma warning restore 618
            serializerSettings.Binder = defaultJsonSerializer.Binder;

            serializerSettings.MaxDepth = Int32.MaxValue;
        }

        internal static void SetUserSerializerSettings(JsonSerializerSettings settings)
        {
            Volatile.Write(ref _userSerializerSettings, settings);
        }

        private static JsonSerializerSettings GetSerializerSettings(SerializationOption serializationOption)
        {
            switch (serializationOption)
            {
                case SerializationOption.Default: return DefaultSerializerSettings;
                case SerializationOption.DefaultWithTypes: return DefaultSerializerSettingsWithTypes;
                case SerializationOption.User: return Volatile.Read(ref _userSerializerSettings);
                default: throw new ArgumentOutOfRangeException(nameof(serializationOption), serializationOption, null);
            }
        }
    }
}
