using System;
using Hangfire.SqlServer.RabbitMQ;
using RabbitMQ.Client;
using Xunit;

namespace Hangfire.SqlServer.RabbitMq.Tests
{
    public class RabbitMqJobQueueProviderFacts
    {
        private const string HostName = "localhost";
        private static readonly string[] Queue = { "default" };

        [Fact]
        public void GetJobQueue_ReturnsNonNullInstance()
        {
            var provider = CreateProvider();

            var jobQueue = provider.GetJobQueue();

            Assert.NotNull(jobQueue);
        }

        [Fact]
        public void GetMonitoringApi_ReturnsNonNullInstance()
        {
            var provider = CreateProvider();

            var monitoring = provider.GetJobQueueMonitoringApi();

            Assert.NotNull(monitoring);
        }

        private static RabbitMqJobQueueProvider CreateProvider()
        {
            ConnectionFactory configuration = new ConnectionFactory { HostName = HostName };

            return new RabbitMqJobQueueProvider(Queue, configuration);
        }
    }
}
