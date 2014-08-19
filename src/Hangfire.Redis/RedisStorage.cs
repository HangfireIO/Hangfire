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
using Common.Logging;
using Hangfire.Redis.Annotations;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using ServiceStack.Redis;

namespace Hangfire.Redis
{
    public class RedisStorage : JobStorage
    {
        internal static readonly string Prefix = "hangfire:";

        private readonly PooledRedisClientManager _pooledManager;

        public RedisStorage()
            : this(String.Format("{0}:{1}", RedisNativeClient.DefaultHost, RedisNativeClient.DefaultPort))
        {
        }

        public RedisStorage(string hostAndPort)
            : this(hostAndPort, (int)RedisNativeClient.DefaultDb)
        {
        }

        public RedisStorage(string hostAndPort, int db)
            : this(hostAndPort, db, new RedisStorageOptions())
        {
        }

        public RedisStorage(string hostAndPort, int db, RedisStorageOptions options)
        {
            if (hostAndPort == null) throw new ArgumentNullException("hostAndPort");
            if (options == null) throw new ArgumentNullException("options");

            HostAndPort = hostAndPort;
            Db = db;
            Options = options;

            _pooledManager = new PooledRedisClientManager(
                new []{ HostAndPort },
                new string[0],
                new RedisClientManagerConfig
                {
                    DefaultDb = Db,
                    MaxWritePoolSize = Options.ConnectionPoolSize
                });
        }

        public string HostAndPort { get; private set; }
        public int Db { get; private set; }
        public RedisStorageOptions Options { get; private set; }

        public PooledRedisClientManager PooledManager { get { return _pooledManager; } }

        public override IMonitoringApi GetMonitoringApi()
        {
            return new RedisMonitoringApi(_pooledManager);
        }

        public override IStorageConnection GetConnection()
        {
            return new RedisConnection(_pooledManager.GetClient());
        }

        public override IEnumerable<IServerComponent> GetComponents()
        {
            yield return new FetchedJobsWatcher(this, Options.InvisibilityTimeout);
        }

        public override IEnumerable<IStateHandler> GetStateHandlers()
        {
            yield return new FailedStateHandler();
            yield return new ProcessingStateHandler();
            yield return new SucceededStateHandler();
            yield return new DeletedStateHandler();
        }

        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info("Using the following options for Redis job storage:");
            logger.InfoFormat("    Connection pool size: {0}.", Options.ConnectionPoolSize);
        }

        public override string ToString()
        {
            return String.Format("redis://{0}/{1}", HostAndPort, Db);
        }

        internal static string GetRedisKey([NotNull] string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            return Prefix + key;
        }
    }
}