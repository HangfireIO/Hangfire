using System;
using System.Linq;

namespace HangFire
{
    internal static class JobHelper
    {
        public static string GetQueueName(Type workerType)
        {
            var attribute = workerType
                .GetCustomAttributes(true)
                .Cast<QueueNameAttribute>()
                .FirstOrDefault();

            return attribute != null ? attribute.Name : "default";
        }
    }
}
