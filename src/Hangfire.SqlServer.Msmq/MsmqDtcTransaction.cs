// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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