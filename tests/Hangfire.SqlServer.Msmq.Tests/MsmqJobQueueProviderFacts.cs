using Xunit;

namespace Hangfire.SqlServer.Msmq.Tests
{
    public class MsmqJobQueueProviderFacts
    {
        private static readonly string[] Queues = { "default" };
            
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

        private static MsmqJobQueueProvider CreateProvider()
        {
            return new MsmqJobQueueProvider(
                CleanMsmqQueueAttribute.PathPattern,
                Queues,
                MsmqTransactionType.Internal);
        }
    }
}
