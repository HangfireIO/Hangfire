using System.Collections.Generic;
using Hangfire.SqlServer.RabbitMQ;
using RabbitMQ.Client;

namespace Hangfire.SqlServer.RabbitMq.Tests
{
    public class RabbitMqChannel
    {
        private readonly IEnumerable<string> _queues;

        public RabbitMqChannel(IEnumerable<string> queues)
        {
            _queues = queues;
            ConnectionFactory = new ConnectionFactory { HostName = "localhost" };
        }

        public ConnectionFactory ConnectionFactory { get; private set; }

        public RabbitMqJobQueue CreateQueue()
        {
            return new RabbitMqJobQueue(_queues, ConnectionFactory);
        }
    }
}