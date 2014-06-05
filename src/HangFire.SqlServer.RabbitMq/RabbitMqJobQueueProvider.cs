using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using HangFire.SqlServer;
using RabbitMQ.Client;

namespace HangFire.RabbitMQ
{
    internal class RabbitMqJobQueueProvider : IPersistentJobQueueProvider
    {
        private readonly RabbitMqJobQueue _jobQueue;
        private readonly RabbitMqMonitoringApi _monitoringApi;

        public RabbitMqJobQueueProvider(IEnumerable<string> queues, Action<ConnectionFactory> configureAction)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (configureAction == null) throw new ArgumentNullException("configureAction");

            ConnectionFactory factory = new ConnectionFactory();
            configureAction(factory);

            _jobQueue = new RabbitMqJobQueue(queues, factory);
            _monitoringApi = new RabbitMqMonitoringApi(factory, queues.ToArray());
        }

        public IPersistentJobQueue GetJobQueue(IDbConnection connection)
        {
            return _jobQueue;
        }

        public IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi(IDbConnection connection)
        {
            return _monitoringApi;
        }
    }
}
