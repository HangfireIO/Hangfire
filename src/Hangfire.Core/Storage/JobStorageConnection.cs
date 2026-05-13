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
using Hangfire.Storage.Monitoring;

namespace Hangfire.Storage
{
    public abstract class JobStorageConnection : IStorageConnection
    {
        private const int DefaultResourceEventLimit = 1000;

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        // Common
        public abstract IWriteOnlyTransaction CreateWriteTransaction();
        public abstract IDisposable AcquireDistributedLock(string resource, TimeSpan timeout);

        // Background jobs
        public abstract string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn);
        public abstract IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken);
        public virtual IFetchedJob FetchNextJob(string tenantId, QueueDescriptor[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", nameof(queues));
            if (tenantId != null)
            {
                throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.Connection.TenantAwareQueueFetch);
            }

            return FetchNextJob(queues.Select(static queue => queue.Name).ToArray(), cancellationToken);
        }
        public abstract void SetJobParameter(string id, string name, string value);
        public abstract string GetJobParameter(string id, string name);
        public abstract JobData GetJobData(string jobId);
        public abstract StateData GetStateData(string jobId);

        // Servers
        public abstract void AnnounceServer(string serverId, ServerContext context);
        public virtual void UpdateServer(string serverId, ServerContext context)
        {
        }

        public abstract void RemoveServer(string serverId);
        public abstract void Heartbeat(string serverId);
        public abstract int RemoveTimedOutServers(TimeSpan timeOut);

        // Sets
        public abstract HashSet<string> GetAllItemsFromSet(string key);
        public abstract string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore);

        public virtual List<string> GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore, int count)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.Connection.BatchedGetFirstByLowest);
        }

        public virtual long GetSetCount([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual long GetSetCount([NotNull] IEnumerable<string> keys, int limit)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.Connection.LimitedGetSetCount);
        }

        public virtual bool GetSetContains([NotNull] string key, [NotNull] string value)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.Connection.GetSetContains);
        }

        public virtual List<string> GetRangeFromSet([NotNull] string key, int startingFrom, int endingAt)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual TimeSpan GetSetTtl([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        // Hashes
        public abstract void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs);
        public abstract Dictionary<string, string> GetAllEntriesFromHash(string key);

        public virtual string GetValueFromHash([NotNull] string key, [NotNull] string name)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual long GetHashCount([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual TimeSpan GetHashTtl([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        // Lists
        public virtual long GetListCount([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual List<string> GetAllItemsFromList([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual List<string> GetRangeFromList([NotNull] string key, int startingFrom, int endingAt)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual TimeSpan GetListTtl([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        // Counters
        public virtual long GetCounter([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual DateTime GetUtcDateTime()
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.Connection.GetUtcDateTime);
        } 

        public virtual ServerResourceCommand GetServerResourceCommand([NotNull] string serverId)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));

            var entries = GetAllEntriesFromHash(GetServerResourceCommandKey(serverId));
            if (entries == null || entries.Count == 0) return null;

            return new ServerResourceCommand
            {
                CommandId = GetValueOrDefault(entries, "commandId"),
                Command = GetValueOrDefault(entries, "command"),
                ServerId = GetValueOrDefault(entries, "target") ?? serverId,
                Queue = GetValueOrDefault(entries, "queue"),
                Reason = GetValueOrDefault(entries, "reason"),
                CreatedAt = ParseDateTime(GetValueOrDefault(entries, "createdAt")),
                CreatedBy = GetValueOrDefault(entries, "createdBy")
            };
        }

        public virtual void SaveServerResourceCommand([NotNull] string serverId, [NotNull] ServerResourceCommand command)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (String.IsNullOrWhiteSpace(command.Command)) throw new ArgumentException("Command name must be specified.", nameof(command));

            var commandId = String.IsNullOrWhiteSpace(command.CommandId) ? Guid.NewGuid().ToString("N") : command.CommandId;
            var createdAt = command.CreatedAt == default(DateTime) ? DateTime.UtcNow : command.CreatedAt.ToUniversalTime();

            using (var transaction = CreateWriteTransaction())
            {
                transaction.SetRangeInHash(
                    GetServerResourceCommandKey(serverId),
                    new[]
                    {
                        new KeyValuePair<string, string>("commandId", commandId),
                        new KeyValuePair<string, string>("command", command.Command),
                        new KeyValuePair<string, string>("target", serverId),
                        new KeyValuePair<string, string>("queue", command.Queue),
                        new KeyValuePair<string, string>("reason", command.Reason),
                        new KeyValuePair<string, string>("createdAt", JobHelper.SerializeDateTime(createdAt)),
                        new KeyValuePair<string, string>("createdBy", command.CreatedBy)
                    });

                transaction.Commit();
            }
        }

        public virtual void ClearServerResourceCommand([NotNull] string serverId, [CanBeNull] string commandId)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));

            if (!String.IsNullOrWhiteSpace(commandId))
            {
                var command = GetServerResourceCommand(serverId);
                if (command != null && !String.Equals(command.CommandId, commandId, StringComparison.Ordinal))
                {
                    return;
                }
            }

            using (var transaction = CreateWriteTransaction())
            {
                transaction.RemoveHash(GetServerResourceCommandKey(serverId));
                transaction.Commit();
            }
        }

        public virtual void AddServerResourceEvent([NotNull] ServerResourceEvent resourceEvent)
        {
            if (resourceEvent == null) throw new ArgumentNullException(nameof(resourceEvent));
            if (String.IsNullOrWhiteSpace(resourceEvent.EventType)) throw new ArgumentException("Event type must be specified.", nameof(resourceEvent));

            if (resourceEvent.CreatedAt == default(DateTime))
            {
                resourceEvent.CreatedAt = DateTime.UtcNow;
            }

            var serialized = SerializationHelper.Serialize(resourceEvent);

            using (var transaction = CreateWriteTransaction())
            {
                if (!String.IsNullOrWhiteSpace(resourceEvent.ServerId))
                {
                    InsertAndTrim(transaction, $"resource-events:server:{resourceEvent.ServerId}", serialized);
                }

                if (!String.IsNullOrWhiteSpace(resourceEvent.Queue))
                {
                    InsertAndTrim(transaction, $"resource-events:queue:{resourceEvent.Queue}", serialized);
                }

                InsertAndTrim(transaction, "resource-events:recent", serialized);
                transaction.Commit();
            }
        }

        public virtual IList<ServerResourceEvent> GetServerResourceEvents([NotNull] string serverId, int from, int count)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));
            if (from < 0) throw new ArgumentOutOfRangeException(nameof(from));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            return DeserializeResourceEvents(GetRangeFromList($"resource-events:server:{serverId}", from, from + count - 1));
        }

        public virtual IList<ServerResourceEvent> GetServerResourceEvents(DateTime from, DateTime to)
        {
            var fromUtc = from.ToUniversalTime();
            var toUtc = to.ToUniversalTime();

            return DeserializeResourceEvents(GetRangeFromList("resource-events:recent", 0, DefaultResourceEventLimit - 1))
                .Where(resourceEvent => resourceEvent.CreatedAt >= fromUtc && resourceEvent.CreatedAt <= toUtc)
                .ToList();
        }

        public virtual IList<QueueAvailabilityDto> GetQueueAvailability(IList<ServerDto> servers, DateTime now, TimeSpan serverTimeout)
        {
            if (servers == null) throw new ArgumentNullException(nameof(servers));

            var result = new Dictionary<string, QueueAvailabilityDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var server in servers)
            {
                if (server.Queues == null) continue;

                var offline = !server.Heartbeat.HasValue || server.Heartbeat.Value < now.Subtract(serverTimeout);
                foreach (var queue in server.Queues)
                {
                    if (String.IsNullOrWhiteSpace(queue)) continue;

                    var key = $"{server.TenantId ?? String.Empty}:{queue}";
                    if (!result.TryGetValue(key, out var availability))
                    {
                        availability = new QueueAvailabilityDto
                        {
                            TenantId = server.TenantId,
                            Queue = queue,
                            ConstrainedByReason = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                        };
                        result.Add(key, availability);
                    }

                    if (offline)
                    {
                        availability.OfflineServers++;
                        continue;
                    }

                    var canAllocate = server.CanAllocate && !server.DrainMode;
                    var reason = server.AllocationReason;
                    if (server.QueueAllocation != null &&
                        server.QueueAllocation.TryGetValue(queue, out var queueAllocation) &&
                        !queueAllocation.CanAllocate)
                    {
                        canAllocate = false;
                        reason = queueAllocation.Reason;
                        if (queueAllocation.DrainMode)
                        {
                            availability.DrainingServers++;
                        }
                    }
                    else if (server.DrainMode)
                    {
                        availability.DrainingServers++;
                    }

                    if (canAllocate)
                    {
                        availability.AvailableServers++;
                    }
                    else
                    {
                        availability.ConstrainedServers++;
                        IncrementReason(availability, String.IsNullOrWhiteSpace(reason) ? "Resource constrained" : reason);
                    }
                }
            }

            return result.Values
                .OrderBy(static x => x.TenantId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static x => x.Queue, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string GetServerResourceCommandKey(string serverId)
        {
            return $"server:{serverId}:resource-command";
        }

        private static string GetValueOrDefault(IDictionary<string, string> entries, string key)
        {
            return entries.TryGetValue(key, out var value) ? value : null;
        }

        private static DateTime ParseDateTime(string value)
        {
            return String.IsNullOrWhiteSpace(value)
                ? DateTime.UtcNow
                : JobHelper.DeserializeDateTime(value);
        }

        private static void InsertAndTrim(IWriteOnlyTransaction transaction, string key, string value)
        {
            transaction.InsertToList(key, value);
            transaction.TrimList(key, 0, DefaultResourceEventLimit - 1);
        }

        private static IList<ServerResourceEvent> DeserializeResourceEvents(IEnumerable<string> items)
        {
            if (items == null) return new List<ServerResourceEvent>();

            return items
                .Select(static item => SerializationHelper.Deserialize<ServerResourceEvent>(item))
                .Where(static item => item != null)
                .ToList();
        }

        private static void IncrementReason(QueueAvailabilityDto availability, string reason)
        {
            if (!availability.ConstrainedByReason.TryGetValue(reason, out var count))
            {
                availability.ConstrainedByReason.Add(reason, 1);
            }
            else
            {
                availability.ConstrainedByReason[reason] = count + 1;
            }
        }
    }
}
