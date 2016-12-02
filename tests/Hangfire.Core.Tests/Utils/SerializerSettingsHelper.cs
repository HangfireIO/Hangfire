using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;

namespace Hangfire.Core.Tests
{
    public static class SerializerSettingsHelper
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