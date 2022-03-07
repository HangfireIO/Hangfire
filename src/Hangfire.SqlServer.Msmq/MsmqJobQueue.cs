// This file is part of Hangfire. Copyright � 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;
using System.Data;
using System.Messaging;
using System.Threading;
using Hangfire.Storage;

namespace Hangfire.SqlServer.Msmq
{
    internal class MsmqJobQueue : IPersistentJobQueue
    {
        private static readonly TimeSpan SyncReceiveTimeout = TimeSpan.FromSeconds(5);

        private readonly string _pathPattern;
        private readonly MsmqTransactionType _transactionType;

        public MsmqJobQueue(string pathPattern, MsmqTransactionType transactionType)
        {
            if (pathPattern == null) throw new ArgumentNullException(nameof(pathPattern));

            _pathPattern = pathPattern;
            _transactionType = transactionType;
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            string jobId = null;
            var queueIndex = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var transaction = CreateTransaction();
                
                try
                {
                    using (var messageQueue = GetMessageQueue(queues[queueIndex]))
                    {
                        var message = queueIndex == queues.Length - 1
                            ? transaction.Receive(messageQueue, SyncReceiveTimeout)
                            : transaction.Receive(messageQueue, new TimeSpan(1));

                        jobId = message.Label;

                        return new MsmqFetchedJob(transaction, jobId);
                    }
                }
                catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    // Receive timeout occurred, we should just switch to the next queue
                }
                finally
                {
                    if (jobId == null)
                    {
                        transaction.Dispose();
                    }
                }

                queueIndex = (queueIndex + 1) % queues.Length;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        public void Enqueue(IDbConnection connection, string queue, string jobId)
        {
            using (var messageQueue = GetMessageQueue(queue))
            using (var message = new Message { Label = jobId })
            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();
                messageQueue.Send(message, transaction);
                transaction.Commit();
            }
        }

        private IMsmqTransaction CreateTransaction()
        {
            switch (_transactionType)
            {
                case MsmqTransactionType.Internal:
                    return new MsmqInternalTransaction();
                case MsmqTransactionType.Dtc:
                    return new MsmqDtcTransaction();
            }

            throw new InvalidOperationException("Unknown MSMQ transaction type: " + _transactionType);
        }

        private MessageQueue GetMessageQueue(string queue)
        {
            return new MessageQueue(String.Format(_pathPattern, queue));
        }
    }
}