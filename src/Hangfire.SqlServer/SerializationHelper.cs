using System.Globalization;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hangfire.SqlServer
{
    internal static class SerializationHelper
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
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
            Binder = new DefaultSerializationBinder(),
        };

        public static string Serialize(object value)
        {
            return value != null
                ? JsonConvert.SerializeObject(value, SerializerSettings)
                : null;
        }

        public static T Deserialize<T>(string value)
        {
            return value != null
               ? JsonConvert.DeserializeObject<T>(value, SerializerSettings)
               : default(T);
        }
    }
}