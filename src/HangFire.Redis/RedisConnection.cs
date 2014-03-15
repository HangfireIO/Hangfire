using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HangFire.Common;
using HangFire.Server;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis
{
    internal class RedisConnection : IStorageConnection
    {
        private readonly IRedisClient _redis;

        public RedisConnection(JobStorage storage, IRedisClient redis)
        {
            _redis = redis;
            Storage = storage;
        }

        public JobStorage Storage { get; private set; }

        public void Dispose()
        {
            _redis.Dispose();
        }

        public IWriteOnlyTransaction CreateWriteTransaction()
        {
            return new RedisWriteOnlyTransaction(_redis.CreateTransaction());
        }

        public IJobFetcher CreateFetcher(IEnumerable<string> queueNames)
        {
            return new RedisJobFetcher(_redis, queueNames, TimeSpan.FromSeconds(1));
        }

        public IDisposable AcquireJobLock(string jobId)
        {
            return _redis.AcquireLock(
                RedisStorage.Prefix + String.Format("job:{0}:state-lock", jobId),
                TimeSpan.FromMinutes(1));
        }

        public string CreateExpiredJob(
            InvocationData invocationData,
            string[] arguments,
            IDictionary<string, string> parameters, 
            TimeSpan expireIn)
        {
            var jobId = Guid.NewGuid().ToString();

            parameters.Add("Type", invocationData.Type);
            parameters.Add("Method", invocationData.Method);
            parameters.Add("ParameterTypes", invocationData.ParameterTypes);
            parameters.Add("Arguments", JobHelper.ToJson(arguments));
            parameters.Add("CreatedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow));

            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format(RedisStorage.Prefix + "job:{0}", jobId),
                    parameters));

                transaction.QueueCommand(x => x.ExpireEntryIn(
                    String.Format(RedisStorage.Prefix + "job:{0}", jobId),
                    expireIn));

                // TODO: check return value
                transaction.Commit();
            }

            return jobId;
        }

        public StateAndInvocationData GetJobStateAndInvocationData(string id)
        {
            var jobData = _redis.GetAllEntriesFromHash(
                String.Format(RedisStorage.Prefix + "job:{0}", id));

            if (jobData.Count == 0) return null;

            var invocationData = new InvocationData();
            if (jobData.ContainsKey("Type"))
            {
                invocationData.Type = jobData["Type"];
            }
            if (jobData.ContainsKey("Method"))
            {
                invocationData.Method = jobData["Method"];
            }
            if (jobData.ContainsKey("ParameterTypes"))
            {
                invocationData.ParameterTypes = jobData["ParameterTypes"];
            }

            return new StateAndInvocationData
            {
                InvocationData = invocationData,
                State = jobData.ContainsKey("State") ? jobData["State"] : null,
            };
        }

        public void SetJobParameter(string id, string name, string value)
        {
            _redis.SetEntryInHash(
                String.Format(RedisStorage.Prefix + "job:{0}", id),
                name,
                value);
        }

        public string GetJobParameter(string id, string name)
        {
            return _redis.GetValueFromHash(
                String.Format(RedisStorage.Prefix + "job:{0}", id),
                name);
        }

        public void CompleteJob(JobPayload payload)
        {
            RedisConnection.RemoveFromDequeuedList(_redis, payload.Queue, payload.Id);
        }

        public string GetFirstByLowestScoreFromSet(string key, long fromScore, long toScore)
        {
            return _redis.GetRangeFromSortedSetByLowestScore(
                RedisStorage.Prefix + key, fromScore, toScore, 0, 1)
                .FirstOrDefault();
        }

        public void AnnounceServer(string serverId, int workerCount, IEnumerable<string> queues)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.AddItemToSet(
                    RedisStorage.Prefix + "servers", serverId));

                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format(RedisStorage.Prefix + "server:{0}", serverId),
                    new Dictionary<string, string>
                        {
                            { "WorkerCount", workerCount.ToString(CultureInfo.InvariantCulture) },
                            { "StartedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                        }));

                foreach (var queue in queues)
                {
                    var queue1 = queue;
                    transaction.QueueCommand(x => x.AddItemToList(
                        String.Format(RedisStorage.Prefix + "server:{0}:queues", serverId),
                        queue1));
                }

                transaction.Commit();
            }
        }

        public void RemoveServer(string serverId)
        {
            RemoveServer(_redis, serverId);
        }

        public static void RemoveServer(IRedisClient redis, string serverId)
        {
            using (var transaction = redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromSet(
                    RedisStorage.Prefix + "servers",
                    serverId));

                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format(RedisStorage.Prefix + "server:{0}", serverId),
                    String.Format(RedisStorage.Prefix + "server:{0}:queues", serverId)));

                transaction.Commit();
            }
        }

        public void Heartbeat(string serverId)
        {
            _redis.SetEntryInHash(
                String.Format(RedisStorage.Prefix + "server:{0}", serverId),
                "Heartbeat",
                JobHelper.ToStringTimestamp(DateTime.UtcNow));
        }

        public int RemoveTimedOutServers(TimeSpan timeOut)
        {
            var serverNames = _redis.GetAllItemsFromSet(RedisStorage.Prefix + "servers");
            var heartbeats = new Dictionary<string, Tuple<DateTime, DateTime?>>();

            var utcNow = DateTime.UtcNow;

            using (var pipeline = _redis.CreatePipeline())
            {
                foreach (var serverName in serverNames)
                {
                    var name = serverName;

                    pipeline.QueueCommand(
                        x => x.GetValuesFromHash(
                            String.Format(RedisStorage.Prefix + "server:{0}", name),
                            "StartedAt", "Heartbeat"),
                        x => heartbeats.Add(
                            name,
                            new Tuple<DateTime, DateTime?>(
                                JobHelper.FromStringTimestamp(x[0]),
                                JobHelper.FromNullableStringTimestamp(x[1]))));
                }

                pipeline.Flush();
            }

            var removedServerCount = 0;
            foreach (var heartbeat in heartbeats)
            {
                var maxTime = new DateTime(
                    Math.Max(heartbeat.Value.Item1.Ticks, (heartbeat.Value.Item2 ?? DateTime.MinValue).Ticks));

                if (utcNow > maxTime.Add(timeOut))
                {
                    RemoveServer(_redis, heartbeat.Key);
                    removedServerCount++;
                }
            }

            return removedServerCount;
        }

        public static void RemoveFromDequeuedList(
            IRedisClient redis,
            string queue,
            string jobId)
        {
            using (var transaction = redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromList(
                    String.Format(RedisStorage.Prefix + "queue:{0}:dequeued", queue),
                    jobId,
                    -1));

                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format(RedisStorage.Prefix + "job:{0}", jobId),
                    "Fetched"));
                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format(RedisStorage.Prefix + "job:{0}", jobId),
                    "Checked"));

                transaction.Commit();
            }
        }
    }
}