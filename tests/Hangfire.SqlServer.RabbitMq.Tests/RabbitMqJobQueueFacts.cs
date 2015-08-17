using System;
using System.Data;
using System.Threading;
using Hangfire.SqlServer.RabbitMQ;
using Moq;
using Xunit;

namespace Hangfire.SqlServer.RabbitMq.Tests
{
    public class RabbitMqJobQueueFacts
    {
        private readonly CancellationToken _token;

        public RabbitMqJobQueueFacts()
        {
            _token = new CancellationTokenSource().Token;
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RabbitMqJobQueue(null, null));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RabbitMqJobQueue(new[]{"my-queue"}, null));

            Assert.Equal("factory", exception.ParamName);
        }

        [FactWithTimeout, CleanRabbitMqQueue("my-queue")]
        public void Enqueue_SendsTheJobId()
        {
            // Arrange
            using (var queue = CleanRabbitMqQueueAttribute.GetMessageQueue("my-queue"))
            {
                // Act
                queue.Enqueue(new Mock<IDbConnection>().Object, "my-queue", "job-id");

                // Assert
                var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token);
                fetchedJob.RemoveFromQueue();

                var body = fetchedJob.JobId;

                Assert.Equal("job-id", body);
            }
        }

        [FactWithTimeout, CleanRabbitMqQueue("my-queue")]
        public void Dequeue_ReturnsFetchedJob_WithJobId()
        {
            RabbitMqUtils.EnqueueJobId("my-queue", "job-id");

            using (var queue = CleanRabbitMqQueueAttribute.GetMessageQueue("my-queue"))
            {
                var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token);
                fetchedJob.RemoveFromQueue();

                Assert.Equal("job-id", fetchedJob.JobId);
            }
        }

        [FactWithTimeout, CleanRabbitMqQueue("my-queue")]
        public void Dequeue_ThrowsCanceledException_WhenTokenHasBeenCancelled()
        {
            using (var queue = CleanRabbitMqQueueAttribute.GetMessageQueue("my-queue"))
            {
                var token = new CancellationToken(true);

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(new[] {"my-queue"}, token));
            }
        }

        [FactWithTimeout, CleanRabbitMqQueue("queue-1", "queue-2")]
        public void Dequeue_ReturnsFetchedJob_FromOtherQueues_IfFirstAreEmpty()
        {
            RabbitMqUtils.EnqueueJobId("queue-2", "job-id");
            using (var queue = CleanRabbitMqQueueAttribute.GetMessageQueue("queue-1", "queue-2"))
            {
                var fetchedJob = queue.Dequeue(new[] {"queue-1", "queue-2"}, _token);

                Assert.Equal("job-id", fetchedJob.JobId);
            }
        }

        [FactWithTimeout, CleanRabbitMqQueue("my-queue")]
        public void Dequeue_MakesJobInvisibleForOtherFetchers()
        {
            // Arrange
            RabbitMqUtils.EnqueueJobId("my-queue", "job-id9");

            using (var queue = CleanRabbitMqQueueAttribute.GetMessageQueue("my-queue"))
            {
                // Act
                var fetchedJob = queue.Dequeue(new[] {"my-queue"}, _token);

                // Assert
                Assert.NotNull(fetchedJob);

                var exception = Assert.Throws<TimeoutException>(
                    () => RabbitMqUtils.DequeueJobId("my-queue", TimeSpan.FromSeconds(1)));

                Assert.Equal(exception.Message, "my-queue");
            }
        }

        [FactWithTimeout, CleanRabbitMqQueue("my-queue")]
        public void RemoveFromQueue_OnFetchedJob_RemovesTheJobCompletely()
        {
            // Arrange
            RabbitMqUtils.EnqueueJobId("my-queue", "job-id");
            using (var queue = CleanRabbitMqQueueAttribute.GetMessageQueue("my-queue"))
            {
                // Act
                using (var fetchedJob = queue.Dequeue(new[] {"my-queue"}, _token))
                {
                    fetchedJob.RemoveFromQueue();
                }

                // Assert
                var exception = Assert.Throws<TimeoutException>(
                    () => RabbitMqUtils.DequeueJobId("my-queue", TimeSpan.FromSeconds(1)));

                Assert.Equal(exception.Message, "my-queue");
            }
        }

        [FactWithTimeout, CleanRabbitMqQueue("my-queue")]
        public void DisposeWithoutRemoval_OnFetchedJob_ReturnsTheJobToTheQueue()
        {
            // Arrange
            RabbitMqUtils.EnqueueJobId("my-queue", "job-id");

            using (var queue = CleanRabbitMqQueueAttribute.GetMessageQueue("my-queue"))
            {
                // Act
                var fetchedJob = queue.Dequeue(new[] {"my-queue"}, _token);
                fetchedJob.Dispose();

                // Assert
                var jobId = RabbitMqUtils.DequeueJobId("my-queue", TimeSpan.FromSeconds(5));
                Assert.Equal("job-id", jobId);
            }
        }

        [FactWithTimeout, CleanRabbitMqQueue("my-queue")]
        public void DisposeWithoutRemoval_OnFetchedJob_CanBeDequeuedAgain()
        {
            // Arrange
            RabbitMqUtils.EnqueueJobId("my-queue", "job-id");

            using (var queue = CleanRabbitMqQueueAttribute.GetMessageQueue("my-queue"))
            {
                // Act
                var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token);
                fetchedJob.Dispose();

                // Assert
                var newFetchedJob = queue.Dequeue(new[] { "my-queue" }, _token);

                Assert.Equal("job-id", newFetchedJob.JobId);
            }
        }
    }
}
