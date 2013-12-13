using System.Threading;
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

        [BeforeScenario("redis")]
        public static void BeforeRedisScenario()
        {
            GlobalLock.Acquire();
            LogManager.LogFactory = new ConsoleLogFactory();

            RedisFactory.Db = RedisDb;
            RedisFactory.Host = RedisHost;

            Client = RedisFactory.BasicManager.GetClient();
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
