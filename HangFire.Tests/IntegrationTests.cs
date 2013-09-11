using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Redis;

namespace HangFire.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        private const int RedisDb = 5;
        private const string RedisHost = "localhost";
        private const int RedisPort = 6379;

        private HangFireServer _server;
        private IRedisClient _redis;

        private static readonly object _lock = new object();
        private static bool _performed;

        [TestInitialize]
        public void SetUp()
        {
            Monitor.Enter(_lock);

            _redis = new ServiceStack.Redis.RedisClient(RedisHost, RedisPort, null, RedisDb);
            _redis.FlushDb();

            LogManager.LogFactory = new ConsoleLogFactory();

            HangFireConfiguration.Configure(
                x =>
                {
                    x.RedisDb = RedisDb;
                    x.RedisHost = RedisHost;
                    x.RedisPort = RedisPort;
                });

            _server = new HangFireServer("TestServer", "default", 1, TimeSpan.FromMilliseconds(500));

            _performed = false;
        }

        [TestCleanup]
        public void TearDown()
        {
            try
            {
                _server.Dispose();
                _redis.Dispose();
            }
            finally
            {
                Monitor.Exit(_lock);
            }   
        }

        [TestMethod]
        public void TheJobIsProcessedWithin50ms()
        {
            HangFireClient.PerformAsync<TestJob>();
            Thread.Sleep(TimeSpan.FromMilliseconds(50));

            Assert.IsTrue(_performed);
            Assert.AreEqual(0, HangFireApi.FailedCount());
            Assert.AreEqual(1, HangFireApi.SucceededCount());
        }

        [TestMethod]
        public void TheJobIsFailedWithin50ms()
        {
            HangFireClient.PerformAsync<FailJob>();
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            
            Assert.AreEqual(0, HangFireApi.SucceededCount());
            Assert.AreEqual(1, HangFireApi.FailedCount());
        }

        [TestMethod]
        public void ScheduledJobIsProcessed()
        {
            HangFireClient.PerformIn<TestJob>(TimeSpan.FromSeconds(1));

            Assert.AreEqual(0, HangFireApi.SucceededCount());

            Thread.Sleep(TimeSpan.FromSeconds(2));

            Assert.AreEqual(0, HangFireApi.FailedCount());
            Assert.AreEqual(1, HangFireApi.SucceededCount());
        }

        public class TestJob
        {
            public void Perform()
            {
                _performed = true;
            }
        }

        public class FailJob
        {
            public void Perform()
            {
                throw new Exception();
            }
        }
    }
}
