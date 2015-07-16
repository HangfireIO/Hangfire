using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using RabbitMQ.Client;

namespace Hangfire.SqlServer.RabbitMQ
{
    internal class RabbitMqJobQueueProvider : IPersistentJobQueueProvider
    {
        private readonly RabbitMqJobQueue _jobQueue;
        private readonly RabbitMqMonitoringApi _monitoringApi;

        public RabbitMqJobQueueProvider(IEnumerable<string> queues, ConnectionFactory configureAction)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (configureAction == null) throw new ArgumentNullException("configureAction");

            _jobQueue = new RabbitMqJobQueue(queues, configureAction);
            _monitoringApi = new RabbitMqMonitoringApi(configureAction, queues.ToArray());
        }

        public IPersistentJobQueue GetJobQueue()
        {
            return _jobQueue;
        }

        public IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi()
        {
            return _monitoringApi;
        }
    }
}
