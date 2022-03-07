// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
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