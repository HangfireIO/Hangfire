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

using Hangfire.SqlServer;
using Hangfire.SqlServer.Msmq;
using Hangfire.States;

// ReSharper disable once CheckNamespace
namespace Hangfire
{
    public static class MsmqExtensions
    {
        public static IGlobalConfiguration<SqlServerStorage> UseMsmqQueues(
            this IGlobalConfiguration<SqlServerStorage> configuration,
            string pathPattern, 
            params string[] queues)
        {
            return UseMsmqQueues(configuration, MsmqTransactionType.Internal, pathPattern, queues);
        }

        public static IGlobalConfiguration<SqlServerStorage> UseMsmqQueues(
            this IGlobalConfiguration<SqlServerStorage> configuration,
            MsmqTransactionType transactionType,
            string pathPattern,
            params string[] queues)
        {
            if (queues.Length == 0)
            {
                queues = new[] { EnqueuedState.DefaultQueue };
            }

            var provider = new MsmqJobQueueProvider(pathPattern, queues, transactionType);
            configuration.Entry.QueueProviders.Add(provider, queues);

            return configuration;
        }
    }
}
