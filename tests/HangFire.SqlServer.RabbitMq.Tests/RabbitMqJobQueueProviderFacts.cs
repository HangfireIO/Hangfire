using System;
using HangFire.RabbitMQ;
using RabbitMQ.Client;
using Xunit;

namespace HangFire.SqlServer.RabbitMq.Tests
{
    public class RabbitMqJobQueueProviderFacts
    {
        private const string HostName = "localhost";
        private static readonly string[] Queue = { "default" };

        [Fact]
        public void GetJobQueue_ReturnsNonNullInstance()
        {
            var provider = CreateProvider();

            var jobQueue = provider.GetJobQueue(null);

            Assert.NotNull(jobQueue);
        }

        [Fact]
        public void GetMonitoringApi_ReturnsNonNullInstance()
        {
            var provider = CreateProvider();

            var monitoring = provider.GetJobQueueMonitoringApi(null);

            Assert.NotNull(monitoring);
        }

        private static RabbitMqJobQueueProvider CreateProvider()
        {
            Action<ConnectionFactory> configuration =
                con =>
                {
                    con.HostName = HostName;
                };

            return new RabbitMqJobQueueProvider(Queue, configuration);
        }
    }
}
