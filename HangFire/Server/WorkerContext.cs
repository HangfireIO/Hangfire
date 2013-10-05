using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Server
{
    public class WorkerContext
    {
        private readonly IRedisClient _redis;

        internal WorkerContext(WorkerContext workerContext)
            : this (workerContext.ServerContext, workerContext.WorkerNumber, workerContext._redis)
        {
            Items = workerContext.Items;
        }

        internal WorkerContext(ServerContext serverContext, int workerNumber, IRedisClient redis)
        {
            _redis = redis;
            ServerContext = serverContext;
            WorkerNumber = workerNumber;

            Items = new Dictionary<string, object>();
        }

        public ServerContext ServerContext { get; private set; }
        public int WorkerNumber { get; private set; }
        public IDictionary<string, object> Items { get; private set; }

        public void Redis(Action<IRedisClient> action)
        {
            if (action == null) throw new ArgumentNullException("action");

            lock (_redis)
            {
                action(_redis);
            }
        }
    }
}