using System;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace HangFire
{
    public static class JobHelper
    {
        public const string DefaultQueue = "default";

        // TODO: use this method only in the Web assembly
        public static string TryToGetQueue(string jobType)
        {
            var type = Type.GetType(jobType);
            if (type == null)
            {
                return null;
            }

            return GetQueue(type);
        }

        public static string GetQueue(Type jobType)
        {
            if (jobType == null) throw new ArgumentNullException("jobType");

            var attribute = jobType
                .GetCustomAttributes(true)
                .Cast<QueueAttribute>()
                .FirstOrDefault();

            var queueName = attribute != null 
                ? !String.IsNullOrEmpty(attribute.Name) ? attribute.Name : DefaultQueue
                : DefaultQueue;
            ValidateQueueName(queueName);

            return queueName;
        }

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

        public static void ValidateQueueName(string queue)
        {
            if (!Regex.IsMatch(queue, @"^[a-z0-9_]+$"))
            {
                throw new InvalidOperationException(String.Format(
                    "The queue name must consist of lowercase letters, digits and underscore characters only. Given: '{0}'", queue));
            }
        }
    }
}
