using ServiceStack.Text;

namespace HangFire
{
    internal static class JsonHelper
    {
        public static string Serialize(object value)
        {
            return JsonSerializer.SerializeToString(value);
        }

        public static T Deserialize<T>(string value)
        {
            return JsonSerializer.DeserializeFromString<T>(value);
        }
    }
}
