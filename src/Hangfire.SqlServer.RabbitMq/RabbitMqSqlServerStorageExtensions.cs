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

            // Use configuration from URI, otherwise use properties
            if (conf.Uri != null)
            {
                cf.uri = conf.Uri;
            }
            else
            {
                cf.HostName = conf.HostName;
                cf.Port = conf.Port;
                cf.UserName = conf.Username;
                cf.Password = conf.Password;
                cf.VirtualHost = conf.VirtualHost;
            }

            var provider = new RabbitMqJobQueueProvider(queues, cf);

            storage.QueueProviders.Add(provider, queues);

            return storage;
        }
    }
}
