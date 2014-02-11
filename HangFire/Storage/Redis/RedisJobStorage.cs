using System.Collections.Generic;
using HangFire.Server;
using HangFire.Server.Fetching;
using HangFire.Storage.Redis.Components;
using ServiceStack.Redis;

namespace HangFire.Storage.Redis
{
    public class RedisJobStorage : JobStorage
    {
        private readonly RedisStorageOptions _options;
        private readonly IRedisClientsManager _pooledManager;
        private readonly IRedisClientsManager _basicManager;

        public RedisJobStorage(string host, int db)
            : this(host, db, new RedisStorageOptions())
        {
        }

        public RedisJobStorage(string host, int db, RedisStorageOptions options)
        {
            _options = options;
            _pooledManager = new PooledRedisClientManager(db, host);
            _basicManager = new BasicRedisClientManager(db, host);
        }

        public override IStorageConnection CreateConnection()
        {
            return new RedisStorageConnection(_basicManager.GetClient());
        }

        public override IStorageConnection CreatePooledConnection()
        {
            return new RedisStorageConnection(_pooledManager.GetClient());
        }

        public override IJobFetcher CreateFetcher(
            IEnumerable<string> queues, int workersCount)
        {
            return new PrioritizedJobFetcher(
                _basicManager.GetClient(),
                queues, 
                workersCount, 
                _options.JobDequeueTimeOut);
        }

        public override IEnumerable<IThreadWrappable> GetComponents()
        {
            yield return new SchedulePoller(_basicManager, _options.PollInterval);
            yield return new DequeuedJobsWatcher(_basicManager);
            yield return new ServerWatchdog(_basicManager);
        }
    }
}