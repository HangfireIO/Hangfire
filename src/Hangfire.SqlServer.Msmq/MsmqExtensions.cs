// This file is part of Hangfire. Copyright © 2015 Sergey Odinokov.
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
