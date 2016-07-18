using System;
using System.Linq;
using System.Messaging;
using System.Reflection;
using System.Threading;
using Xunit.Sdk;

namespace Hangfire.SqlServer.Msmq.Tests
{
    public class CleanMsmqQueueAttribute : BeforeAfterTestAttribute
    {
        private static readonly object GlobalLock = new object();

        public static readonly string PathPattern = @".\Private$\hangfire-{0}";

        private readonly string[] _queues;

        public CleanMsmqQueueAttribute(params string[] queues)
        {
            _queues = queues;
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            Monitor.Enter(GlobalLock);

            foreach (var queuePath in _queues.Select(GetPath))
            {
                if (MessageQueue.Exists(queuePath))
                {
                    MessageQueue.Delete(queuePath);
                }

                using (MessageQueue.Create(queuePath, transactional: true))
                {
                    // We just need to create it.
                }
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Monitor.Exit(GlobalLock);
        }

        public static MessageQueue GetMessageQueue(string queue)
        {
            return new MessageQueue(GetPath(queue));
        }

        private static string GetPath(string queue)
        {
            return String.Format(PathPattern, queue);
        }
    }
}
