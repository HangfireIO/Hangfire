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
using System.Collections.Generic;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;

namespace Hangfire.Storage
{
    public interface IStorageConnection : IDisposable
    {
        IWriteOnlyTransaction CreateWriteTransaction();
        IDisposable AcquireDistributedLock(string resource, TimeSpan timeout);

        string CreateExpiredJob(
            Job job, 
            IDictionary<string, string> parameters, 
            DateTime createdAt,
            TimeSpan expireIn);

        IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken);

        void SetJobParameter(string id, string name, string value);
        string GetJobParameter(string id, string name);

        [CanBeNull]
        JobData GetJobData([NotNull] string jobId);

        [CanBeNull]
        StateData GetStateData([NotNull] string jobId);

        void AnnounceServer(string serverId, ServerContext context);
        void RemoveServer(string serverId);
        void Heartbeat(string serverId);
        int RemoveTimedOutServers(TimeSpan timeOut);

        // Set operations

        [NotNull]
        HashSet<string> GetAllItemsFromSet([NotNull] string key);

        string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore);

        // Hash operations

        void SetRangeInHash([NotNull] string key, [NotNull] IEnumerable<KeyValuePair<string, string>> keyValuePairs);

        [CanBeNull]
        Dictionary<string, string> GetAllEntriesFromHash([NotNull] string key);
    }
}