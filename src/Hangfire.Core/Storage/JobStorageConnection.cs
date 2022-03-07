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
    public abstract class JobStorageConnection : IStorageConnection
    {
        public virtual void Dispose()
        {
        }

        // Common
        public abstract IWriteOnlyTransaction CreateWriteTransaction();
        public abstract IDisposable AcquireDistributedLock(string resource, TimeSpan timeout);

        // Background jobs
        public abstract string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn);
        public abstract IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken);
        public abstract void SetJobParameter(string id, string name, string value);
        public abstract string GetJobParameter(string id, string name);
        public abstract JobData GetJobData(string jobId);
        public abstract StateData GetStateData(string jobId);

        // Servers
        public abstract void AnnounceServer(string serverId, ServerContext context);
        public abstract void RemoveServer(string serverId);
        public abstract void Heartbeat(string serverId);
        public abstract int RemoveTimedOutServers(TimeSpan timeOut);

        // Sets
        public abstract HashSet<string> GetAllItemsFromSet(string key);
        public abstract string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore);

        public virtual List<string> GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore, int count)
        {
            throw new NotSupportedException();
        }

        public virtual long GetSetCount([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual List<string> GetRangeFromSet([NotNull] string key, int startingFrom, int endingAt)
        {
            throw new NotSupportedException();
        }

        public virtual TimeSpan GetSetTtl([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        // Hashes
        public abstract void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs);
        public abstract Dictionary<string, string> GetAllEntriesFromHash(string key);

        public virtual string GetValueFromHash([NotNull] string key, [NotNull] string name)
        {
            throw new NotSupportedException();
        }

        public virtual long GetHashCount([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual TimeSpan GetHashTtl([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        // Lists
        public virtual long GetListCount([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual List<string> GetAllItemsFromList([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual List<string> GetRangeFromList([NotNull] string key, int startingFrom, int endingAt)
        {
            throw new NotSupportedException();
        }

        public virtual TimeSpan GetListTtl([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        // Counters
        public virtual long GetCounter([NotNull] string key)
        {
            throw new NotSupportedException();
        }
    }
}