using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Storage;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Redis
{
    public class RedisStorageConnection : IStorageConnection
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RedisStorageConnection));

        private const string Prefix = "hangfire:";
        private readonly IRedisClient _redis;

        public RedisStorageConnection(RedisJobStorage storage, IRedisClient redis)
        {
            _redis = redis;
            
            Jobs = new RedisStoredJobs(redis);
            Sets = new RedisStoredSets(redis);
            Storage = storage;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        public IAtomicWriteTransaction CreateWriteTransaction()
        {
            return new RedisAtomicWriteTransaction(_redis.CreateTransaction());
        }

        public IDisposable AcquireJobLock(string jobId)
        {
            return _redis.AcquireLock(
                Prefix + String.Format("job:{0}:state-lock", jobId),
                TimeSpan.FromMinutes(1));
        }

        public IStoredJobs Jobs { get; private set; }
        public IStoredSets Sets { get; private set; }
        public JobStorage Storage { get; private set; }

        public void AnnounceServer(string serverId, int workerCount, IEnumerable<string> queues)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.AddItemToSet(
                    "hangfire:servers", serverId));

                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:server:{0}", serverId),
                    new Dictionary<string, string>
                        {
                            { "WorkerCount", workerCount.ToString() },
                            { "StartedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                        }));

                foreach (var queue in queues)
                {
                    var queue1 = queue;
                    transaction.QueueCommand(x => x.AddItemToList(
                        String.Format("hangfire:server:{0}:queues", serverId),
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
                    "hangfire:servers",
                    serverId));

                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:server:{0}", serverId),
                    String.Format("hangfire:server:{0}:queues", serverId)));

                transaction.Commit();
            }
        }

        public void Heartbeat(string serverId)
        {
            _redis.SetEntryInHash(
                String.Format("hangfire:server:{0}", serverId),
                "Heartbeat",
                JobHelper.ToStringTimestamp(DateTime.UtcNow));
        }

        public void RemoveTimedOutServers(TimeSpan timeOut)
        {
            var serverNames = _redis.GetAllItemsFromSet("hangfire:servers");
            var heartbeats = new Dictionary<string, Tuple<DateTime, DateTime?>>();

            var utcNow = DateTime.UtcNow;

            using (var pipeline = _redis.CreatePipeline())
            {
                foreach (var serverName in serverNames)
                {
                    var name = serverName;

                    pipeline.QueueCommand(
                        x => x.GetValuesFromHash(
                            String.Format("hangfire:server:{0}", name),
                            "StartedAt", "Heartbeat"),
                        x => heartbeats.Add(
                            name,
                            new Tuple<DateTime, DateTime?>(
                                JobHelper.FromStringTimestamp(x[0]),
                                JobHelper.FromNullableStringTimestamp(x[1]))));
                }

                pipeline.Flush();
            }

            Logger.DebugFormat("Looking for timed out servers...");

            foreach (var heartbeat in heartbeats)
            {
                var maxTime = new DateTime(
                    Math.Max(heartbeat.Value.Item1.Ticks, (heartbeat.Value.Item2 ?? DateTime.MinValue).Ticks));

                if (utcNow > maxTime.Add(timeOut))
                {
                    RemoveServer(_redis, heartbeat.Key);
                    Logger.InfoFormat("Server '{0}' was removed due to time out.", heartbeat.Key);
                }
            }
        }

        public static void RemoveFromDequeuedList(
            IRedisClient redis,
            string queue,
            string jobId)
        {
            using (var transaction = redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromList(
                    String.Format("hangfire:queue:{0}:dequeued", queue),
                    jobId,
                    -1));

                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Fetched"));
                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "Checked"));

                transaction.Commit();
            }
        }
    }
}