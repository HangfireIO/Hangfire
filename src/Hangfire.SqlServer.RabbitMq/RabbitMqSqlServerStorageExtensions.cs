using System;
using RabbitMQ.Client;

namespace Hangfire.SqlServer.RabbitMQ
{
    public static class RabbitMqSqlServerStorageExtensions
    {
        public static SqlServerStorage UseRabbitMq(this SqlServerStorage storage, Action<RabbitMqConnectionConfiguration> configureAction, params string[] queues)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (queues == null) throw new ArgumentNullException("queues");
            if (configureAction == null) throw new ArgumentNullException("configureAction");

            RabbitMqConnectionConfiguration conf = new RabbitMqConnectionConfiguration();
            configureAction(conf);

            ConnectionFactory cf = new ConnectionFactory();
            cf.HostName = conf.HostName;
            cf.Port = conf.Port;
            cf.UserName = conf.Username;
            cf.Password = conf.Password;
            
            var provider = new RabbitMqJobQueueProvider(queues, cf);

            storage.QueueProviders.Add(provider, queues);

            return storage;
        }
    }
}
