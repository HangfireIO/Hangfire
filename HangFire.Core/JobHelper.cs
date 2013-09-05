using System;
using System.Linq;

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
    }
}
