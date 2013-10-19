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

        private static readonly object _lock = new object();

        public static IRedisClient Client;

        [BeforeScenario("redis")]
        public static void BeforeRedisScenario()
        {
            Monitor.Enter(_lock);
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
                Monitor.Exit(_lock);
            }
        }
    }
}
