using System.Threading;
using HangFire.Redis;
using HangFire.Storage;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Redis;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public static class Redis
    {
        private const int RedisDb = 5;
        private const string RedisHost = "localhost:6379";

        public static IRedisClient Client;
        public static RedisStorage Storage;

        [BeforeScenario("redis")]
        public static void BeforeRedisScenario()
        {
            GlobalLock.Acquire();
            LogManager.LogFactory = new ConsoleLogFactory();

            Storage = new RedisStorage(RedisHost, RedisDb);
            JobStorage.Current = Storage;

            Client = Storage.PooledManager.GetClient();
            Client.FlushDb();
        }

        [AfterScenario("redis")]
        public static void AfterRedisScenario()
        {
            try
            {
                Client.Dispose();
            }
            finally
            {
                GlobalLock.Release();
            }
        }
    }
}
