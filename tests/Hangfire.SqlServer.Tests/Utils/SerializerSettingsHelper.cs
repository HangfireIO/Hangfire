using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;

namespace Hangfire.SqlServer.Tests
{
    public class SerializerSettingsHelper
    {
        public static JsonSerializerSettings DangerousSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,

            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,

            Formatting = Formatting.Indented,

            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
        };
    }
}