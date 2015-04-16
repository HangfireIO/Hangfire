using System;
using System.Text;
using Hangfire.Storage;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Hangfire.SqlServer.RabbitMQ
{
    internal class RabbitMqFetchedJob : IFetchedJob
    {
        private readonly BasicDeliverEventArgs _message;
        private IModel _channel;
        private bool _completed;
        private bool _disposed;

        public RabbitMqFetchedJob(BasicDeliverEventArgs message, ref IModel channel)
        {
            if (message == null) throw new ArgumentNullException("message");

            _message = message;
            _channel = channel;

            JobId = Encoding.UTF8.GetString(_message.Body);
        }

        public string JobId { get; private set; }

        public void RemoveFromQueue()
        {
            if (_completed) throw new InvalidOperationException("Job already completed");
            _channel.BasicAck(_message.DeliveryTag, false);
            _completed = true;
        }

        public void Requeue()
        {
            if (_completed) throw new InvalidOperationException("Job already completed");
            _channel.BasicNack(_message.DeliveryTag, false, true);
            _channel.Close(global::RabbitMQ.Client.Framing.Constants.ReplySuccess, "Requeue");

            _completed = true;
        }

        public void Dispose()
        {
            if (!_completed && !_disposed)
            {
                Requeue();
            }

            _disposed = true;
        }
    }
}
