// This file is part of Hangfire. Copyright � 2015 Sergey Odinokov.
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
using System.Messaging;

namespace Hangfire.SqlServer.Msmq
{
    internal class MsmqInternalTransaction : IMsmqTransaction
    {
        private readonly MessageQueueTransaction _transaction;

        public MsmqInternalTransaction()
        {
            _transaction = new MessageQueueTransaction();
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }

        public Message Receive(MessageQueue queue, TimeSpan timeout)
        {
            _transaction.Begin();
            return queue.Receive(timeout, _transaction);
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Abort()
        {
            _transaction.Abort();
        }
    }
}