using System;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HangFire.Tests
{
    [TestClass]
    public class IntegrationTests : RedisPoweredTest
    {
        private HangFireServer _server;
        private static bool _performed;

        protected override void Initialize()
        {
            _server = new HangFireServer("TestServer", "default", 1, TimeSpan.FromMilliseconds(500));
            _performed = false;
        }

        protected override void CleanUp()
        {
            _server.Dispose();
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

        public class TestJob : HangFireJob
        {
            public override void Perform()
            {
                _performed = true;
            }
        }

        public class FailJob : HangFireJob
        {
            public override void Perform()
            {
                throw new Exception();
            }
        }
    }
}
