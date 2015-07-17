// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

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
        private readonly ThreadLocal<IMessageFormatter> _formatter;

        public MsmqJobQueue(string pathPattern, MsmqTransactionType transactionType)
        {
            if (pathPattern == null) throw new ArgumentNullException("pathPattern");

            _pathPattern = pathPattern;
            _transactionType = transactionType;

            _formatter = new ThreadLocal<IMessageFormatter>(
                () => new BinaryMessageFormatter());
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            string jobId = null;
            IMsmqTransaction transaction;
            var queueIndex = 0;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var queueName = queues[queueIndex];

                transaction = CreateTransaction();
                
                using (var messageQueue = GetMessageQueue(queueName))
                {
                    try
                    {
                        var message = queueIndex == queues.Length - 1
                            ? transaction.Receive(messageQueue, SyncReceiveTimeout)
                            : transaction.Receive(messageQueue, new TimeSpan(1));

                        message.Formatter = _formatter.Value;

                        jobId = (string)message.Body;

                    }
                    catch (MessageQueueException ex)
                    {
                        transaction.Dispose();

                        if (ex.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                        {
                            throw;
                        }
                    }
                }

                queueIndex = (queueIndex + 1) % queues.Length;
            } while (jobId == null);

            return new MsmqFetchedJob(transaction, jobId);
        }

        public void Enqueue(IDbConnection connection, string queue, string jobId)
        {
            using (var messageQueue = GetMessageQueue(queue))
            using (var message = new Message { Body = jobId, Label = jobId, Formatter = _formatter.Value })
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