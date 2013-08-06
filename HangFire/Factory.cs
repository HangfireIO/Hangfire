using System;
using ServiceStack.Redis;

namespace HangFire
{
    internal static class Factory
    {
        public static Client CreateClient()
        {
            return new Client(CreateRedisClient());
        }

        private static readonly IRedisClientsManager ClientsManager
            = new PooledRedisClientManager();

        public static IRedisClient CreateRedisClient()
        {
            return ClientsManager.GetClient();
        }

        public static Worker CreateWorker(Type workerType)
        {
            return (Worker)Activator.CreateInstance(workerType);
        }
    }
}
