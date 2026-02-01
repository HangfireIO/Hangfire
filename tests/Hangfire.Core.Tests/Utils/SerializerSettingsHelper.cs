using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;

namespace Hangfire.Core.Tests
{
    public static class SerializerSettingsHelper
    {
        public static JsonSerializerSettings DangerousSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,

#if NET452 || NET461 || NETCOREAPP3_1
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
#else
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
#endif

            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,

            Formatting = Formatting.Indented,

            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
        };
    }
}
