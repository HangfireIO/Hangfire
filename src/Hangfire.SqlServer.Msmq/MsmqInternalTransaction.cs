// This file is part of Hangfire. Copyright © 2015 Hangfire OÜ.
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