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