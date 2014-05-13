// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Messaging;
using System.Threading;
using HangFire.Storage;

namespace HangFire.SqlServer
{
    internal class MsmqJobQueue : IPersistentJobQueue
    {
        private static readonly TimeSpan SyncReceiveTimeout = TimeSpan.FromSeconds(5);

        private readonly string _pathPattern;
        private readonly IMessageFormatter _formatter;

        public MsmqJobQueue(string pathPattern)
        {
            if (pathPattern == null) throw new ArgumentNullException("pathPattern");

            _pathPattern = pathPattern;
            _formatter = new BinaryMessageFormatter();
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            string jobId = null;
            MessageQueueTransaction transaction;
            var queueIndex = 0;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                queueIndex = (queueIndex + 1) % queues.Length;
                var queueName = queues[queueIndex];

                transaction = new MessageQueueTransaction();

                using (var messageQueue = GetMessageQueue(queueName))
                {
                    try
                    {
                        transaction.Begin();

                        var message = queueIndex == 0
                            ? messageQueue.Receive(SyncReceiveTimeout, transaction)
                            : messageQueue.Receive(new TimeSpan(1), transaction);

                        message.Formatter = _formatter;

                        jobId = (string)message.Body;

                    }
                    catch (MessageQueueException ex)
                    {
                        transaction.Abort();
                        transaction.Dispose();

                        if (ex.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                        {
                            throw;
                        }
                    }
                }
            } while (jobId == null);

            return new MsmqFetchedJob(transaction, jobId);
        }

        public void Enqueue(string queue, string jobId)
        {
            using (var messageQueue = GetMessageQueue(queue))
            using (var message = new Message { Body = jobId, Label = jobId, Formatter = _formatter })
            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();
                messageQueue.Send(message, transaction);
                transaction.Commit();
            }
        }

        private MessageQueue GetMessageQueue(string queue)
        {
            var queuePath = String.Format(_pathPattern, queue);

            return new MessageQueue(queuePath);
        }
    }
}