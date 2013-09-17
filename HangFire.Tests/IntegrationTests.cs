using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HangFire.Tests
{
    [TestClass]
    public class IntegrationTests : RedisPoweredTest
    {
        private BackgroundJobServer _server;
        private static bool _performed;

        protected override void Initialize()
        {
            _server = new BackgroundJobServer
                {
                    ServerName = "TestServer",
                    QueueName = "default",
                    WorkersCount = 1,
                    PollInterval = TimeSpan.FromMilliseconds(500)
                };
            _server.Start();
            _performed = false;
        }

        protected override void CleanUp()
        {
            _server.Stop();
        }

        [TestMethod]
        public void TheJobIsProcessedWithin50ms()
        {
            Perform.Async<TestJob>();
            Thread.Sleep(TimeSpan.FromMilliseconds(50));

            Assert.IsTrue(_performed);
            Assert.AreEqual(0, JobStorage.FailedCount());
            Assert.AreEqual(1, JobStorage.SucceededCount());
        }

        [TestMethod]
        public void TheJobIsFailedWithin50ms()
        {
            Perform.Async<FailJob>();
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            
            Assert.AreEqual(0, JobStorage.SucceededCount());
            Assert.AreEqual(1, JobStorage.FailedCount());
        }

        [TestMethod]
        public void ScheduledJobIsProcessed()
        {
            Perform.In<TestJob>(TimeSpan.FromSeconds(1));

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
