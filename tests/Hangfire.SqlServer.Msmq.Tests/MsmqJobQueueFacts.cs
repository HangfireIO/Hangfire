using System;
using System.Data;
using System.Messaging;
using System.Threading;
using Moq;
using Xunit;

// ReSharper disable PossibleNullReferenceException

namespace Hangfire.SqlServer.Msmq.Tests
{
    public class MsmqJobQueueFacts
    {
        private readonly CancellationToken _token;
        private readonly Mock<IDbConnection> _connection;

        public MsmqJobQueueFacts()
        {
            _token = new CancellationToken();
            _connection = new Mock<IDbConnection>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenPathPatternIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new MsmqJobQueue(null, MsmqTransactionType.Internal));

            Assert.Equal("pathPattern", exception.ParamName);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void Enqueue_SendsTheJobId()
        {
            // Arrange
            var queue = CreateQueue(MsmqTransactionType.Internal);

            // Act
            queue.Enqueue(_connection.Object, "my-queue", "job-id");

            // Assert
            using (var messageQueue = CleanMsmqQueueAttribute.GetMessageQueue("my-queue"))
            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();

                var message = messageQueue.Receive(TimeSpan.FromSeconds(5), transaction);

                Assert.Equal("job-id", message.Label);

                transaction.Commit();
            }
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void Enqueue_AddsAJob_WhenIdIsLongValue()
        {
            // Arrange
            var queue = CreateQueue(MsmqTransactionType.Internal);

            // Act
            queue.Enqueue(_connection.Object, "my-queue", (int.MaxValue + 1L).ToString());

            // Assert
            using (var messageQueue = CleanMsmqQueueAttribute.GetMessageQueue("my-queue"))
            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();

                var message = messageQueue.Receive(TimeSpan.FromSeconds(5), transaction);

                Assert.Equal((int.MaxValue + 1L).ToString(), message.Label);

                transaction.Commit();
            }
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void Dequeue_ReturnsFetchedJob_WithJobId()
        {
            MsmqUtils.EnqueueJobId("my-queue", "job-id");
            var queue = CreateQueue(MsmqTransactionType.Internal);

            var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token);

            Assert.Equal("job-id", fetchedJob.JobId);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void Dequeue_ThrowsCanceledException_WhenTokenHasBeenCancelled()
        {
            var queue = CreateQueue(MsmqTransactionType.Internal);
            var token = new CancellationToken(true);

            Assert.Throws<OperationCanceledException>(
                () => queue.Dequeue(new[] { "my-queue" }, token));
        }

        [Fact, CleanMsmqQueue("queue-1", "queue-2")]
        public void Dequeue_ReturnsFetchedJob_FromOtherQueues_IfFirstAreEmpty()
        {
            MsmqUtils.EnqueueJobId("queue-2", "job-id");
            var queue = CreateQueue(MsmqTransactionType.Internal);

            var fetchedJob = queue.Dequeue(new[] { "queue-1", "queue-2" }, _token);

            Assert.Equal("job-id", fetchedJob.JobId);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void Dequeue_MakesJobInvisibleForOtherFetchers()
        {
            // Arrange
            MsmqUtils.EnqueueJobId("my-queue", "job-id");
            var queue = CreateQueue(MsmqTransactionType.Internal);

            // Act
            var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token);

            // Assert
            Assert.NotNull(fetchedJob);

            var exception = Assert.Throws<MessageQueueException>(
                () => MsmqUtils.DequeueJobId("my-queue", TimeSpan.FromSeconds(1)));

            Assert.Equal(MessageQueueErrorCode.IOTimeout, exception.MessageQueueErrorCode);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void RemoveFromQueue_OnFetchedJob_RemovesTheJobCompletely()
        {
            // Arrange
            MsmqUtils.EnqueueJobId("my-queue", "job-id");
            var queue = CreateQueue(MsmqTransactionType.Internal);

            // Act
            using (var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token))
            {
                fetchedJob.RemoveFromQueue();
            }

            // Assert
            var exception = Assert.Throws<MessageQueueException>(
                () => MsmqUtils.DequeueJobId("my-queue", TimeSpan.FromSeconds(5)));

            Assert.Equal(MessageQueueErrorCode.IOTimeout, exception.MessageQueueErrorCode);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void DisposeWithoutRemoval_OnFetchedJob_ReturnsTheJobToTheQueue()
        {
            // Arrange
            MsmqUtils.EnqueueJobId("my-queue", "job-id");
            var queue = CreateQueue(MsmqTransactionType.Internal);

            // Act
            var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token);
            fetchedJob.Dispose();

            // Assert
            var jobId = MsmqUtils.DequeueJobId("my-queue", TimeSpan.FromSeconds(5));
            Assert.Equal("job-id", jobId);
        }

        private static MsmqJobQueue CreateQueue(MsmqTransactionType transactionType)
        {
            return new MsmqJobQueue(CleanMsmqQueueAttribute.PathPattern, transactionType);
        }
    }
}
