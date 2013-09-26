using System;
using System.Collections.Generic;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Server
{
    public class WorkerContext
    {
        private readonly RedisStorage _storage;

        internal WorkerContext(WorkerContext workerContext)
            : this (workerContext.ServerContext, workerContext.WorkerNumber, workerContext._storage)
        {
            Items = workerContext.Items;
        }

        internal WorkerContext(ServerContext serverContext, int workerNumber, RedisStorage storage)
        {
            _storage = storage;
            ServerContext = serverContext;
            WorkerNumber = workerNumber;

            Items = new Dictionary<string, object>();
        }

        public ServerContext ServerContext { get; private set; }
        public int WorkerNumber { get; private set; }
        public IDictionary<string, object> Items { get; private set; }

        public void Redis(Action<IRedisClient> action)
        {
            lock (_storage)
            {
                action(_storage.Redis);
            }
        }
    }
}