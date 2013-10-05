using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Redis;

namespace HangFire.Tests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable"), TestClass]
    public abstract class RedisPoweredTest
    {
        protected const int RedisDb = 5;
        protected const string RedisHost = "localhost";
        protected const int RedisPort = 6379;

        protected IRedisClient Redis;

        private static readonly object _lock = new object();

        [TestInitialize]
        public void SetUp()
        {
            Monitor.Enter(_lock);
            LogManager.LogFactory = new ConsoleLogFactory();

            Redis = new RedisClient(RedisHost, RedisPort, null, RedisDb);
            Redis.FlushDb();

            RedisFactory.Db = RedisDb;
            RedisFactory.Host = RedisHost;
            RedisFactory.Port = RedisPort;

            Initialize();
        }

        [TestCleanup]
        public virtual void TearDown()
        {
            try
            {
                CleanUp();
                Redis.Dispose();
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }

        protected virtual void Initialize()
        {
        }

        protected virtual void CleanUp()
        {
        }
    }
}
