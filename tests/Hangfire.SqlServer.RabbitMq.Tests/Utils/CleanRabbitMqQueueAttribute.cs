using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Hangfire.SqlServer.RabbitMQ;
using Xunit;

namespace Hangfire.SqlServer.RabbitMq.Tests
{
    public class CleanRabbitMqQueueAttribute : BeforeAfterTestAttribute
    {
        private static readonly object GlobalLock = new object();
        private readonly IEnumerable<string> _queues;
        private readonly RabbitMqJobQueue _rabbitMq;

        public CleanRabbitMqQueueAttribute(params string[] queues)
        {
            _queues = queues;
            _rabbitMq = new RabbitMqChannel(_queues).CreateQueue();
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            Monitor.Enter(GlobalLock);
            foreach (var queue in _queues)
            {
                _rabbitMq.Channel.QueueDeclare(queue, true, false, false, null);
                _rabbitMq.Channel.QueuePurge(queue);
            }
            _rabbitMq.Dispose();
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Monitor.Exit(GlobalLock);
        }

        public static RabbitMqJobQueue GetMessageQueue(params string[] queue)
        {
            return new RabbitMqChannel(queue).CreateQueue();
        }
    }
}
