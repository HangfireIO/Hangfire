using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire
{
    public abstract class HangFireJob : IDisposable
    {
        public IDictionary<string, string> Args { get; set; }

        public abstract void Perform();

        public virtual void Dispose()
        {
        }

        internal static string GetQueueName(Type workerType)
        {
            var attribute = workerType
                .GetCustomAttributes(true)
                .Cast<QueueNameAttribute>()
                .FirstOrDefault();

            return attribute != null ? attribute.Name : "default";
        }
    }
}
