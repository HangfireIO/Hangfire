using System;
using System.Collections.Generic;
using HangFire.Common.States;
using HangFire.Redis.Components;
using HangFire.Redis.States;
using HangFire.Server;
using HangFire.Server.Components;
using HangFire.Storage;
using HangFire.Storage.Monitoring;
using ServiceStack.Redis;

namespace HangFire.Redis
{
    public class RedisStorage : JobStorage
    {
        internal static readonly string Prefix = "hangfire:";

        private readonly string _host;
        private readonly int _db;

        private readonly RedisStorageOptions _options;
        private readonly IRedisClientsManager _pooledManager;

        public RedisStorage(string host, int db)
            : this(host, db, new RedisStorageOptions())
        {
        }

        public RedisStorage(string host, int db, RedisStorageOptions options)
        {
            _host = host;
            _db = db;
            _options = options;

            _pooledManager = new PooledRedisClientManager(
                new []{ host },
                new string[0],
                new RedisClientManagerConfig
                {
                    DefaultDb = db,
                    MaxWritePoolSize = _options.ConnectionPoolSize
                });
        }

        public IRedisClientsManager PooledManager { get { return _pooledManager; } }

        public override IMonitoringApi GetMonitoringApi()
        {
            return new RedisMonitoringApi(_pooledManager.GetClient());
        }

        public override IStorageConnection GetConnection()
        {
            return new RedisStorageConnection(this, _pooledManager.GetClient());
        }

        public override IEnumerable<IThreadWrappable> GetComponents()
        {
            yield return new SchedulePoller(this, _options.PollInterval);
            yield return new DequeuedJobsWatcher(this);
            yield return new ServerWatchdog(this);
        }

        public override IEnumerable<JobStateHandler> GetStateHandlers()
        {
            yield return new FailedStateHandler();
            yield return new ProcessingStateHandler();
            yield return new SucceededStateHandler();
        }

        public override string ToString()
        {
            return String.Format("redis://{0}/{1}", _host, _db);
        }
    }
}