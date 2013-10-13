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
        private const string RedisHost = "localhost";
        private const int RedisPort = 6379;

        private static readonly object _lock = new object();

        public static IRedisClient Client;

        [BeforeScenario("redis")]
        public static void BeforeRedisScenario()
        {
            Monitor.Enter(_lock);
            LogManager.LogFactory = new ConsoleLogFactory();

            Client = new RedisClient(RedisHost, RedisPort, null, RedisDb);
            Client.FlushDb();

            RedisFactory.Db = RedisDb;
            RedisFactory.Host = RedisHost;
            RedisFactory.Port = RedisPort;
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
