using System;
using System.Linq;
using Xunit;

namespace Hangfire.SqlServer.Msmq.Tests
{
    public class MsmqJobQueueMonitoringApiFacts
    {
        private static readonly string PathPattern = CleanMsmqQueueAttribute.PathPattern;
        private static readonly string[] Queues = { "default", "critical" };

        [Fact]
        public void Ctor_ThrowsAnException_WhenPathPatternIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new MsmqJobQueueMonitoringApi(null, Queues));

            Assert.Equal("pathPattern", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueuesCollectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new MsmqJobQueueMonitoringApi(PathPattern, null));

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

        [Fact, CleanMsmqQueue("my-queue", "another-queue")]
        public void GetEnqueuedAndFetchedCount_ReturnsCorrectCounters()
        {
            MsmqUtils.EnqueueJobId("my-queue", "1");
            MsmqUtils.EnqueueJobId("my-queue", "2");
            MsmqUtils.EnqueueJobId("another-queue", "3");

            var api = CreateMonitoringApi();

            var result = api.GetEnqueuedAndFetchedCount("my-queue");

            Assert.Equal(2, result.EnqueuedCount);
            Assert.Null(result.FetchedCount);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void GetEnqueuedJobIds_ReturnsEmptyCollection_IfQueueIsEmpty()
        {
            var api = CreateMonitoringApi();

            var result = api.GetEnqueuedJobIds("my-queue", 5, 15);

            Assert.Empty(result);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void GetEnqueuedJobIds_ReturnsCorrectResult()
        {
            for (var i = 1; i <= 10; i++) { MsmqUtils.EnqueueJobId("my-queue", i.ToString()); }
            var api = CreateMonitoringApi();

            var result = api.GetEnqueuedJobIds("my-queue", 3, 2).ToArray();

            Assert.Equal(2, result.Length);
            Assert.Equal(4, result[0]);
            Assert.Equal(5, result[1]);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void GetEnqueuedJobIds_ReturnsCorrectResult_WhenJobIdIsLongValue()
        {
            MsmqUtils.EnqueueJobId("my-queue", (int.MaxValue + 1L).ToString());

            var api = CreateMonitoringApi();

            var result = api.GetEnqueuedJobIds("my-queue", 0, 1).ToArray();

            Assert.Equal(1, result.Length);
            Assert.Equal(int.MaxValue + 1L, result[0]);
        }

        private static MsmqJobQueueMonitoringApi CreateMonitoringApi()
        {
            return new MsmqJobQueueMonitoringApi(PathPattern, Queues);
        }
    }
}
