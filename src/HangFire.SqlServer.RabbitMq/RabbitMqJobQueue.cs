using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using HangFire.SqlServer;
using HangFire.Storage;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HangFire.RabbitMQ
{
    public class RabbitMqJobQueue : IPersistentJobQueue, IDisposable
    {
        private static readonly int SyncReceiveTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;
        private static readonly object ConsumerLock = new object();
        private readonly IEnumerable<string> _queues;
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IModel _channel;
        private Dictionary<string, QueueingBasicConsumer> _consumers;

        public RabbitMqJobQueue(IEnumerable<string> queues, ConnectionFactory factory)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (factory == null) throw new ArgumentNullException("factory");

            _queues = queues;
            _factory = factory;
            _connection = factory.CreateConnection();
            _consumers = new Dictionary<string, QueueingBasicConsumer>();

            CreateChannel();
        }

        public IModel Channel
        {
            get { return _channel; }
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            BasicDeliverEventArgs message;
            var queueIndex = 0;

            do
            {
                queueIndex = (queueIndex + 1) % queues.Length;
                var queueName = queues[queueIndex];

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var consumer = GetConsumerForQueue(queueName, cancellationToken);
                    consumer.Queue.Dequeue(SyncReceiveTimeout, out message);
                }
                catch (global::RabbitMQ.Client.Exceptions.AlreadyClosedException)
                {
                    CreateChannel();
                    message = null;
                }
                catch (System.IO.EndOfStreamException)
                {
                    CreateChannel();
                    message = null;
                }

            } while (message == null);

            return new RabbitMqFetchedJob(message, ref _channel);
        }

        public void Enqueue(string queue, string jobId)
        {
            var body = Encoding.UTF8.GetBytes(jobId);
            var properties = _channel.CreateBasicProperties();
            properties.SetPersistent(true);

            _channel.BasicPublish("", queue, properties, body);
        }

        public void Dispose()
        {
            if (_channel != null)
            {
                if (_channel.IsOpen) _channel.Close();
                _channel.Dispose();
            }

            if (_connection != null)
            {
                if (_connection.IsOpen) _connection.Close();
                _connection.Dispose();
            }
        }

        private void CreateChannel()
        {
            lock (ConsumerLock)
            {
                if (_channel != null && _channel.IsOpen && _connection.IsOpen) return;

                if (_channel != null && _channel.IsOpen) _channel.Abort();
                if (!_connection.IsOpen) _connection = _factory.CreateConnection();

                _channel = _connection.CreateModel();
                _channel.BasicQos(0, 1, false);

                var properties = _channel.CreateBasicProperties();
                properties.SetPersistent(true);

                // QueueDeclare is idempotent
                foreach (var queue in _queues)
                    _channel.QueueDeclare(queue, true, false, false, null);
            }
        }

        private QueueingBasicConsumer GetConsumerForQueue(string queue, CancellationToken cancellationToken)
        {
            QueueingBasicConsumer consumer;
            if (!_consumers.TryGetValue(queue, out consumer))
            {
                consumer = new QueueingBasicConsumer(_channel);
                _consumers.Add(queue, consumer);
            }
            else
            {
                if (consumer.Model.IsClosed)
                {
                    // Recreate the consumer with the new channel
                    consumer = new QueueingBasicConsumer(_channel);
                }
            }

            if (!consumer.IsRunning)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Will throw AlreadyClosedException if consumer channel is closed
                _channel.BasicConsume(queue, false, "HangFire.RabbitMq." + Thread.CurrentThread.Name, consumer);
            }

            return consumer;
        }
    }
}
