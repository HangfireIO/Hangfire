using System;
using ServiceStack.Text;

namespace HangFire
{
    public static class JobHelper
    {
        public static string ToJson(object value)
        {
            return JsonSerializer.SerializeToString(value);
        }

        public static T FromJson<T>(string value)
        {
            return JsonSerializer.DeserializeFromString<T>(value);
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime FromTimestamp(long value)
        {
            return Epoch.AddSeconds(value);
        }

        public static string ToStringTimestamp(DateTime value)
        {
            return ToTimestamp(value).ToString();
        }

        public static DateTime FromStringTimestamp(string value)
        {
            return FromTimestamp(long.Parse(value));
        }

        public static DateTime? FromNullableStringTimestamp(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            return FromStringTimestamp(value);
        }
    }
}
