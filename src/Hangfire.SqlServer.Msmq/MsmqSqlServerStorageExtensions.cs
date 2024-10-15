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
using Hangfire.Annotations;
using Hangfire.States;

namespace Hangfire.SqlServer.Msmq
{
    public static class MsmqSqlServerStorageExtensions
    {
        public static SqlServerStorage UseMsmqQueues(
            [NotNull] this SqlServerStorage storage,
            [NotNull] string pathPattern)
        {
            return UseMsmqQueues(storage, pathPattern, EnqueuedState.DefaultQueue);
        }

        public static SqlServerStorage UseMsmqQueues(
            [NotNull] this SqlServerStorage storage,
            [NotNull] string pathPattern,
            params string[] queues)
        {
            return UseMsmqQueues(storage, MsmqTransactionType.Internal, pathPattern, queues);
        }

        public static SqlServerStorage UseMsmqQueues(
            [NotNull] this SqlServerStorage storage, 
            MsmqTransactionType transactionType,
            [NotNull] string pathPattern, 
            params string[] queues)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            if (queues.Length == 0)
            {
                queues = new[] { EnqueuedState.DefaultQueue }; 
            }

            var provider = new MsmqJobQueueProvider(pathPattern, queues, transactionType);
            storage.QueueProviders.Add(provider, queues);

            return storage;
        }
    }
}