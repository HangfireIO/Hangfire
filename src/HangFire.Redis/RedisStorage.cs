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
        private readonly string _host;
        private readonly int _db;

        private readonly RedisStorageOptions _options;
        private readonly IRedisClientsManager _pooledManager;
        private readonly IRedisClientsManager _basicManager;

        public RedisStorage(string host, int db)
            : this(host, db, new RedisStorageOptions())
        {
        }

        public RedisStorage(string host, int db, RedisStorageOptions options)
        {
            _host = host;
            _db = db;
            _options = options;

            _pooledManager = new PooledRedisClientManager(db, host);
            _basicManager = new BasicRedisClientManager(db, host);
        }

        public IRedisClientsManager BasicManager { get { return _basicManager; } }
        public IRedisClientsManager PooledManager { get { return _pooledManager; } }

        public override IMonitoringApi CreateMonitoring()
        {
            return new RedisMonitoringApi(this, _basicManager.GetClient());
        }

        public override IStorageConnection CreateConnection()
        {
            return new RedisStorageConnection(this, _basicManager.GetClient());
        }

        public override IStorageConnection CreatePooledConnection()
        {
            return new RedisStorageConnection(this, _pooledManager.GetClient());
        }

        public override IEnumerable<IThreadWrappable> GetComponents()
        {
            yield return new SchedulePoller(CreateConnection(), _options.PollInterval);
            yield return new DequeuedJobsWatcher(this, _basicManager);
            yield return new ServerWatchdog(CreateConnection());
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