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

// ReSharper disable RedundantNullnessAttributeWithNullableReferenceTypes
#nullable enable

namespace Hangfire.Storage
{
    public interface IStorageConnection : IDisposable
    {
        [NotNull] IWriteOnlyTransaction CreateWriteTransaction();
        [NotNull] IDisposable AcquireDistributedLock([NotNull] string resource, TimeSpan timeout);

        [CanBeNull]
        string? CreateExpiredJob(
            [NotNull] Job job, 
            [NotNull] IDictionary<string, string?> parameters, 
            DateTime createdAt,
            TimeSpan expireIn);

        [NotNull]
        IFetchedJob FetchNextJob([NotNull] string[] queues, CancellationToken cancellationToken);

        void SetJobParameter([NotNull] string id, [NotNull] string name, [CanBeNull] string? value);

        [CanBeNull]
        string? GetJobParameter([NotNull] string id, [NotNull] string name);

        [CanBeNull]
        JobData? GetJobData([NotNull] string jobId);

        [CanBeNull]
        StateData? GetStateData([NotNull] string jobId);

        void AnnounceServer([NotNull] string serverId, [NotNull] ServerContext context);
        void RemoveServer([NotNull] string serverId);
        void Heartbeat([NotNull] string serverId);
        int RemoveTimedOutServers(TimeSpan timeOut);

        // Set operations

        [NotNull]
        HashSet<string> GetAllItemsFromSet([NotNull] string key);

        [CanBeNull]
        string? GetFirstByLowestScoreFromSet([NotNull] string key, double fromScore, double toScore);

        // Hash operations

        // TODO: Replace IEnumerable with IReadOnlyDictionary to avoid possible key duplicates
        void SetRangeInHash([NotNull] string key, [NotNull] IEnumerable<KeyValuePair<string, string?>> keyValuePairs);

        [CanBeNull]
        Dictionary<string, string?>? GetAllEntriesFromHash([NotNull] string key);
    }
}