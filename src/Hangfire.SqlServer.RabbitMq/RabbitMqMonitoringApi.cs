using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;

namespace Hangfire.SqlServer.RabbitMQ
{
    internal class RabbitMqMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly IEnumerable<string> _queues;
        private readonly ConnectionFactory _factory;

        public RabbitMqMonitoringApi(ConnectionFactory factory, params string[] queues)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (factory == null) throw new ArgumentNullException("factory");

            _queues = queues;
            _factory = factory;
        }

        public IEnumerable<string> GetQueues()
        {
            return _queues;
        }

        /// <remarks>
        /// RabbitMq does not have a Peek feature, the solution is to dequeue all messages
        /// without acknowledging them (noAck = true). After all messages have been read
        /// we dispose the RabbitMqJobQueue causing the channel to close. All unack'd
        /// messages then get requeued in order.
        /// </remarks>
        public IEnumerable<int> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            using (var client = new RabbitMqJobQueue(new[] {queue}, _factory))
            {
                var consumer = new Subscription(client.Channel, queue, true);

                List<int> jobIds = new List<int>();
                BasicDeliverEventArgs delivery;

                while (consumer.Next(1000, out delivery))
                {
                    var body = Encoding.UTF8.GetString(delivery.Body);
                    jobIds.Add(Convert.ToInt32(body));
                }

                return jobIds.Skip(@from).Take(perPage);
            }
        }

        public IEnumerable<int> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            return Enumerable.Empty<int>();
        }

        /// <remarks>
        /// Calling QueueDeclare will return the number of messages that exist in the queue.
        /// QueueDeclare is idempotent so it can be called regardless if the queue exists.
        /// </remarks>
        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            using (var client = new RabbitMqJobQueue(new[] {queue}, _factory))
            {
                var channel = client.Channel.QueueDeclare(queue, true, false, false, null);

                return new EnqueuedAndFetchedCountDto
                {
                    EnqueuedCount = (int) channel.MessageCount
                };
            }
        }
    }
}
