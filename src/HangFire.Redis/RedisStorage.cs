// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

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
        private readonly PooledRedisClientManager _pooledManager;

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

        public PooledRedisClientManager PooledManager { get { return _pooledManager; } }

        public override IMonitoringApi GetMonitoringApi()
        {
            return new RedisMonitoringApi(_pooledManager.GetClient());
        }

        public override IStorageConnection GetConnection()
        {
            return new RedisConnection(this, _pooledManager.GetClient());
        }

        public override IEnumerable<IThreadWrappable> GetComponents()
        {
            yield return new SchedulePoller(this, _options.PollInterval);
            yield return new FetchedJobsWatcher(this);
            yield return new ServerWatchdog(this);
        }

        public override IEnumerable<StateHandler> GetStateHandlers()
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