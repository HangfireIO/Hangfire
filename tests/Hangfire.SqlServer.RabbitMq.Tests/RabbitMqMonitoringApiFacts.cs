using System;
using System.Linq;
using Hangfire.SqlServer.RabbitMQ;
using Xunit;

namespace Hangfire.SqlServer.RabbitMq.Tests
{
    public class RabbitMqMonitoringApiFacts
    {
        private static readonly string[] Queues = {"default"};

        [Fact]
        public void Ctor_ThrowsAnException_WhenFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RabbitMqMonitoringApi(null, Queues));

            Assert.Equal("factory", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RabbitMqMonitoringApi(null, null));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact]
        public void GetQueues_ReturnsAllGivenQueues()
        {
            var api = CreateMonitoringApi();

            var queues = api.GetQueues();

            Assert.Equal(Queues, queues);
        }

        [Fact]
        public void GetFetchedJobIds_ReturnsEmptyCollection()
        {
            var api = CreateMonitoringApi();

            var fetchedJobIds = api.GetFetchedJobIds("", 1, 14);

            Assert.Empty(fetchedJobIds);
        }

        [FactWithTimeout, CleanRabbitMqQueue("my-queue")]
        public void GetEnqueuedAndFetchedCount_ReturnsCorrectCounters()
        {
            RabbitMqUtils.EnqueueJobId("my-queue", "1");
            RabbitMqUtils.EnqueueJobId("my-queue", "2");
            RabbitMqUtils.EnqueueJobId("my-queue", "3");

            var api = CreateMonitoringApi();

            var result = api.GetEnqueuedAndFetchedCount("my-queue");

            Assert.Equal(3, result.EnqueuedCount);
            Assert.Null(result.FetchedCount);
        }

        [FactWithTimeout, CleanRabbitMqQueue("my-queue")]
        public void GetEnqueuedJobIds_ReturnsEmptyCollection_IfQueueIsEmpty()
        {
            var api = CreateMonitoringApi();

            var result = api.GetEnqueuedJobIds("my-queue", 5, 15);

            Assert.Empty(result);
        }

        [FactWithTimeout, CleanRabbitMqQueue("my-queue")]
        public void GetEnqueuedJobIds_ReturnsCorrectResult()
        {
            for (var i = 1; i <= 10; i++)
            {
                RabbitMqUtils.EnqueueJobId("my-queue", i.ToString());
            }

            var api = CreateMonitoringApi();

            var result = api.GetEnqueuedJobIds("my-queue", 3, 2).ToArray();

            Assert.Equal(2, result.Length);
            Assert.Equal(4, result[0]);
            Assert.Equal(5, result[1]);
        }

        private static RabbitMqMonitoringApi CreateMonitoringApi()
        {
            return new RabbitMqMonitoringApi(new RabbitMqChannel(Queues).ConnectionFactory, Queues);
        }
    }
}
