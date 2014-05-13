using System;
using System.Messaging;
using System.Threading;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class MsmqJobQueueFacts
    {
        private readonly CancellationToken _token;

        public MsmqJobQueueFacts()
        {
            _token = new CancellationToken();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenPathPatternIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new MsmqJobQueue(null));

            Assert.Equal("pathPattern", exception.ParamName);
        }

        [Fact, CleanMsmqQueue("my-queue"), CleanDatabase]
        public void Enqueue_SendsTheJobId()
        {
            // Arrange
            var queue = CreateQueue();

            // Act
            queue.Enqueue("my-queue", "job-id");

            // Assert
            using (var messageQueue = CleanMsmqQueueAttribute.GetMessageQueue("my-queue"))
            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();

                var message = messageQueue.Receive(TimeSpan.FromSeconds(5), transaction);
                message.Formatter = new BinaryMessageFormatter();

                Assert.Equal("job-id", message.Body);
                Assert.Equal("job-id", message.Label);

                transaction.Commit();
            }
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void Dequeue_ReturnsFetchedJob_WithJobId()
        {
            EnqueueJobId("my-queue", "job-id");
            var queue = CreateQueue();

            var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token);

            Assert.Equal("job-id", fetchedJob.JobId);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void Dequeue_ThrowsCanceledException_WhenTokenHasBeenCancelled()
        {
            var queue = CreateQueue();
            var token = new CancellationToken(true);

            Assert.Throws<OperationCanceledException>(
                () => queue.Dequeue(new[] { "my-queue" }, token));
        }

        [Fact, CleanMsmqQueue("queue-1", "queue-2")]
        public void Dequeue_ReturnsFetchedJob_FromOtherQueues_IfFirstAreEmpty()
        {
            EnqueueJobId("queue-2", "job-id");
            var queue = CreateQueue();

            var fetchedJob = queue.Dequeue(new[] { "queue-1", "queue-2" }, _token);

            Assert.Equal("job-id", fetchedJob.JobId);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void Dequeue_MakesJobInvisibleForOtherFetchers()
        {
            // Arrange
            EnqueueJobId("my-queue", "job-id");
            var queue = CreateQueue();

            // Act
            var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token);

            // Assert
            Assert.NotNull(fetchedJob);

            var exception = Assert.Throws<MessageQueueException>(
                () => DequeueJob("my-queue", TimeSpan.FromSeconds(1)));

            Assert.Equal(MessageQueueErrorCode.IOTimeout, exception.MessageQueueErrorCode);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void RemoveFromQueue_OnFetchedJob_RemovesTheJobCompletely()
        {
            // Arrange
            EnqueueJobId("my-queue", "job-id");
            var queue = CreateQueue();

            // Act
            using (var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token))
            {
                fetchedJob.RemoveFromQueue();
            }

            // Assert
            var exception = Assert.Throws<MessageQueueException>(
                () => DequeueJob("my-queue", TimeSpan.FromSeconds(5)));

            Assert.Equal(MessageQueueErrorCode.IOTimeout, exception.MessageQueueErrorCode);
        }

        [Fact, CleanMsmqQueue("my-queue")]
        public void DisposeWithoutRemoval_OnFetchedJob_ReturnsTheJobToTheQueue()
        {
            // Arrange
            EnqueueJobId("my-queue", "job-id");
            var queue = CreateQueue();

            // Act
            var fetchedJob = queue.Dequeue(new[] { "my-queue" }, _token);
            fetchedJob.Dispose();

            // Assert
            var jobId = DequeueJob("my-queue", TimeSpan.FromSeconds(5));
            Assert.Equal("job-id", jobId);
        }

        private static void EnqueueJobId(string queue, string jobId)
        {
            using (var messageQueue = CleanMsmqQueueAttribute.GetMessageQueue(queue))
            using (var message = new Message { Body = jobId, Label = jobId, Formatter = new BinaryMessageFormatter() })
            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();
                messageQueue.Send(message, transaction);
                transaction.Commit();
            }
        }

        private static string DequeueJob(string queue, TimeSpan timeout)
        {
            using (var messageQueue = CleanMsmqQueueAttribute.GetMessageQueue(queue))
            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();
                
                using (var message = messageQueue.Receive(timeout, transaction))
                {
                    message.Formatter = new BinaryMessageFormatter();
                    transaction.Commit();

                    return (string) message.Body;
                }
            }
        }

        private static MsmqJobQueue CreateQueue()
        {
            return new MsmqJobQueue(CleanMsmqQueueAttribute.PathPattern);
        }
    }
}
