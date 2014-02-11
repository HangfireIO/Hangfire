using System.Threading;
using HangFire.Storage;
using HangFire.Storage.Redis;
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
        public static RedisJobStorage Storage;

        [BeforeScenario("redis")]
        public static void BeforeRedisScenario()
        {
            GlobalLock.Acquire();
            LogManager.LogFactory = new ConsoleLogFactory();

            Storage = new RedisJobStorage(RedisHost, RedisDb);
            JobStorage.SetCurrent(Storage);

            Client = Storage.BasicManager.GetClient();
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
