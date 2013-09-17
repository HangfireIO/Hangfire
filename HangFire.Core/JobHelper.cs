using System;
using System.Linq;

using ServiceStack.Text;

namespace HangFire
{
    internal static class JobHelper
    {
        public static string GetQueueName(string jobType)
        {
            // TODO: what to do with type resolving exceptions?
            var type = Type.GetType(jobType);

            return GetQueueName(type);
        }

        public static string GetQueueName(Type jobType)
        {
            var attribute = jobType
                .GetCustomAttributes(true)
                .Cast<QueueNameAttribute>()
                .FirstOrDefault();

            return attribute != null ? attribute.Name : "default";
        }

        public static string ToJson(object value)
        {
            return JsonSerializer.SerializeToString(value);
        }

        public static T FromJson<T>(string value)
        {
            return JsonSerializer.DeserializeFromString<T>(value);
        }
    }
}
