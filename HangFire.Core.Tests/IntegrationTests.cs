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
            Assert.AreEqual(0, JobStorage.FailedCount());
            Assert.AreEqual(1, JobStorage.SucceededCount());
        }

        [TestMethod]
        public void TheJobIsFailedWithin50ms()
        {
            HangFireClient.PerformAsync<FailJob>();
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            
            Assert.AreEqual(0, JobStorage.SucceededCount());
            Assert.AreEqual(1, JobStorage.FailedCount());
        }

        [TestMethod]
        public void ScheduledJobIsProcessed()
        {
            HangFireClient.PerformIn<TestJob>(TimeSpan.FromSeconds(1));

            Assert.AreEqual(0, JobStorage.SucceededCount());

            Thread.Sleep(TimeSpan.FromSeconds(2));

            Assert.AreEqual(0, JobStorage.FailedCount());
            Assert.AreEqual(1, JobStorage.SucceededCount());
        }

        public class TestJob : BackgroundJob
        {
            public override void Perform()
            {
                _performed = true;
            }
        }

        public class FailJob : BackgroundJob
        {
            public override void Perform()
            {
                throw new Exception();
            }
        }
    }
}
