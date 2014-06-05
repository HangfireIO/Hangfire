using System;
using HangFire.SqlServer;
using RabbitMQ.Client;

namespace HangFire.RabbitMQ
{
    public static class RabbitMqSqlServerStorageExtensions
    {
        public static SqlServerStorage UseRabbitMq(this SqlServerStorage storage, Action<ConnectionFactory> configureAction, params string[] queues)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (queues == null) throw new ArgumentNullException("queues");
            if (configureAction == null) throw new ArgumentNullException("configureAction");

            var provider = new RabbitMqJobQueueProvider(queues, configureAction);

            storage.QueueProviders.Add(provider, queues);

            return storage;
        }
    }
}
