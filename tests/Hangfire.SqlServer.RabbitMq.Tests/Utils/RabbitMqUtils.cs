using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Hangfire.SqlServer.RabbitMq.Tests
{
    internal class RabbitMqUtils
    {
        public static void EnqueueJobId(string queue, string jobId)
        {
            using (var messageQueue = CleanRabbitMqQueueAttribute.GetMessageQueue(queue))
            {
                var body = Encoding.UTF8.GetBytes(jobId);

                var properties = messageQueue.Channel.CreateBasicProperties();
                properties.SetPersistent(true);

                messageQueue.Channel.BasicPublish("", queue, properties, body);
            }
        }

        public static string DequeueJobId(string queue, TimeSpan timeout)
        {
            int timeoutMilliseconds = (int)timeout.TotalMilliseconds;

            using (var messageQueue = CleanRabbitMqQueueAttribute.GetMessageQueue(queue))
            {
                messageQueue.Channel.BasicQos(0, 1, false);
                var consumer = new QueueingBasicConsumer(messageQueue.Channel);
                messageQueue.Channel.BasicConsume(queue, false, consumer);

                BasicDeliverEventArgs message;
                bool dequeued = consumer.Queue.Dequeue(timeoutMilliseconds, out message);
                if (dequeued == false) throw new TimeoutException(queue);

                return Encoding.UTF8.GetString(message.Body);
            }
        }
    }
}
