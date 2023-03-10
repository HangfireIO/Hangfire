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
using System.Collections.Generic;
using System.Linq;
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

        [Obsolete("Please use/override the GetSetCount method that results `long` instead. Will be removed in 1.8.0.")]
        public virtual KeyValuePair<string, long>[] GetSetCount([NotNull] string[] keys, int limit)
        {
            throw new NotSupportedException();
        }

        public virtual long GetSetCount([NotNull] IEnumerable<string> keys, int limit)
        {
#pragma warning disable CS0618
            return GetSetCount(keys.ToArray(), limit).Sum(x => x.Value);
#pragma warning restore CS0618
        }

        [Obsolete("Please use/override the GetSetContains method instead. Will be removed in 1.8.0.")]
        public virtual bool SetContains([NotNull] string key, [NotNull] string value)
        {
            throw new NotSupportedException();
        }

        public virtual bool GetSetContains([NotNull] string key, [NotNull] string value)
        {
#pragma warning disable CS0618
            return SetContains(key, value);
#pragma warning restore CS0618
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

        public virtual DateTime GetUtcDateTime()
        {
            throw new NotSupportedException();
        } 
    }
}