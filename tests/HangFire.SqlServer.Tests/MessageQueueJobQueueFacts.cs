using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Messaging;
using System.Threading;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class MessageQueueJobQueueFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenPathPatternIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new MessageQueueJobQueue(null));

            Assert.Equal("pathPattern", exception.ParamName);
        }

        [Fact, CleanMsmqQueue("my-queue"), CleanDatabase]
        public void Enqueue_AddsAction_ThatSendsTheJobId()
        {
            // Arrange
            var actions = new Queue<Action<SqlConnection>>();
            var queue = CreateQueue();

            // Act
            queue.Enqueue(actions, "my-queue", "job-id");

            foreach (var action in actions)
            {
                action(ConnectionUtils.CreateConnection());
            }

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
        public void Dequeue_ReturnsProcessingJob_WithJobId()
        {
            EnqueueJobId("my-queue", "job-id");
            var queue = CreateQueue();
            var token = new CancellationToken();

            var processingJob = queue.Dequeue(new[] { "my-queue" }, token);

            Assert.Equal("job-id", processingJob.JobId);
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

        private static MessageQueueJobQueue CreateQueue()
        {
            return new MessageQueueJobQueue(CleanMsmqQueueAttribute.PathPattern);
        }
    }
}
