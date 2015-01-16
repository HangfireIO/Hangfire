// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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

        public abstract IWriteOnlyTransaction CreateWriteTransaction();
        public abstract IDisposable AcquireDistributedLock(string resource, TimeSpan timeout);
        public abstract string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn);
        public abstract IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken);
        public abstract void SetJobParameter(string id, string name, string value);
        public abstract string GetJobParameter(string id, string name);
        public abstract JobData GetJobData(string jobId);
        public abstract StateData GetStateData(string jobId);
        public abstract void AnnounceServer(string serverId, ServerContext context);
        public abstract void RemoveServer(string serverId);
        public abstract void Heartbeat(string serverId);
        public abstract int RemoveTimedOutServers(TimeSpan timeOut);
        public abstract HashSet<string> GetAllItemsFromSet(string key);
        public abstract string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore);
        public abstract void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs);
        public abstract Dictionary<string, string> GetAllEntriesFromHash(string key);

        public virtual string GetValueFromHash([NotNull] string key, [NotNull] string name)
        {
            throw new NotSupportedException();
        }

        public virtual long GetSetCount([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual long GetListCount([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public List<string> GetRangeFromList([NotNull] string key, int startingFrom, int endingAt)
        {
            throw new NotSupportedException();
        }

        public List<string> GetRangeFromSet([NotNull] string key, int startingFrom, int endingAt)
        {
            throw new NotSupportedException();
        }
    }
}