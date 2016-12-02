using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Hangfire.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
        private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings();
        private static readonly JsonSerializerSettings DefaultSerializerSettingsWithTypes = new JsonSerializerSettings();
        private static JsonSerializerSettings _userSerializerSettings;

        static SerializationHelper()
        {
            ApplyDefaultSerializerSettings(DefaultSerializerSettings);
            ApplyDefaultSerializerSettings(DefaultSerializerSettingsWithTypes, TypeNameHandling.Objects);
        }

        /// <summary>
        /// Sets settings for user data serialization like arguments and parameters.
        /// Use <see cref="Serialize(object, SerializationOption)"/> with <see cref="SerializationOption.User"/> option
        /// to serialize with user settings
        /// </summary>
        public static void SetUserSerializerSettings(JsonSerializerSettings settings)
        {
            _userSerializerSettings = settings;
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
        /// Use this method to deserialze internal data. Using isolated settings that can't be changed from user code.
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
            catch (Exception jsonException)
            {
                try
                {
                    if (option == SerializationOption.User) throw;
                    //It's here for backward compatability. Earlier internal data serializer used user setting.
                    return Deserialize(value, type, SerializationOption.User);
                }
                catch (Exception)
                {
                    ExceptionDispatchInfo.Capture(jsonException).Throw();
                    throw;
                }
            }
        }

        /// <summary>
        /// Deserializes data with <see cref="SerializationOption.Default"/> option.
        /// Use this method to deserialze internal data. Using isolated settings that can't be changed from user code.
        /// </summary>
        public static T Deserialize<T>(string value)
        {
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

            var serializerSettings = GetSerializerSettings(option);

            try
            {
                return JsonConvert.DeserializeObject<T>(value, serializerSettings);
            }
            catch (Exception jsonException)
            {
                try
                {
                    if (option == SerializationOption.User) throw;

                    //It's here for backward compatability. Earlier internal data serializer used user setting.
                    return Deserialize<T>(value, SerializationOption.User);
                }
                catch (Exception)
                {
                    ExceptionDispatchInfo.Capture(jsonException).Throw();
                    throw;
                }
            }
        }

        internal static void ApplyDefaultSerializerSettings(
            [NotNull] JsonSerializerSettings serializerSettings,
            TypeNameHandling typeNameHandling = TypeNameHandling.None)
        {
            if (serializerSettings == null) throw new ArgumentNullException(nameof(serializerSettings));

            //It's necessary to set default values explicitly in order to `JsonConvert.DefaultSettings` don't affect hangfire serialization.
            //See more http://www.newtonsoft.com/json/help/html/DefaultSettings.htm and 
            //https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/JsonConvert.cs#L63

            serializerSettings.TypeNameHandling = typeNameHandling;
            serializerSettings.TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple;
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
            serializerSettings.Binder = new DefaultSerializationBinder();
            serializerSettings.Context = new StreamingContext();
        }

        private static JsonSerializerSettings GetSerializerSettings(SerializationOption serializationOption)
        {
            switch (serializationOption)
            {
                case SerializationOption.Default:
                    return DefaultSerializerSettings;
                case SerializationOption.DefaultWithTypes:
                    return DefaultSerializerSettingsWithTypes;
                case SerializationOption.User:
                    return _userSerializerSettings;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serializationOption), serializationOption, null);
            }
        }
    }
}