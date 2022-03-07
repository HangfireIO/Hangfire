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
using System.Messaging;
using System.Transactions;

namespace Hangfire.SqlServer.Msmq
{
    internal class MsmqDtcTransaction : IMsmqTransaction
    {
        private readonly TransactionScope _scope;
        private TransactionScope _suppressedScope;

        public MsmqDtcTransaction()
        {
            _scope = new TransactionScope(TransactionScopeOption.Required, TimeSpan.Zero);
        }

        public void Dispose()
        {
            if (_suppressedScope != null)
            {
                _suppressedScope.Complete();
                _suppressedScope.Dispose();
            }

            _scope.Dispose();
        }

        public Message Receive(MessageQueue queue, TimeSpan timeout)
        {
            var message = queue.Receive(timeout, MessageQueueTransactionType.Automatic);
            _suppressedScope = new TransactionScope(TransactionScopeOption.Suppress, TimeSpan.Zero);

            return message;
        }

        public void Commit()
        {
            _scope.Complete();
        }

        public void Abort()
        {
        }
    }
}