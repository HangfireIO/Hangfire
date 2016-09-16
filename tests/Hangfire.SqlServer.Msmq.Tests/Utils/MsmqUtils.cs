using System;
using System.Messaging;

// ReSharper disable PossibleNullReferenceException

namespace Hangfire.SqlServer.Msmq.Tests
{
    internal class MsmqUtils
    {
        public static void EnqueueJobId(string queue, string jobId)
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

        public static string DequeueJobId(string queue, TimeSpan timeout)
        {
            using (var messageQueue = CleanMsmqQueueAttribute.GetMessageQueue(queue))
            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();

                using (var message = messageQueue.Receive(timeout, transaction))
                {
                    message.Formatter = new BinaryMessageFormatter();
                    transaction.Commit();

                    return (string)message.Body;
                }
            }
        }
    }
}
