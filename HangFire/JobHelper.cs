using System;
using System.Linq;

using ServiceStack.Text;

namespace HangFire
{
    internal static class JobHelper
    {
        public static string TryToGetQueueName(string jobType)
        {
            var type = Type.GetType(jobType);
            if (type == null)
            {
                return null;
            }

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
