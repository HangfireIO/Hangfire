using System;
using HangFire.Server;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class WorkerManagerSmokeTest
    {
        private const string Server = "server";
        private static readonly string[] Queues = { "default" };

        private readonly Mock<JobStorage> _storage = new Mock<JobStorage>();

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new WorkerManager(null, Server, Queues, 1));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new WorkerManager(_storage.Object, null, Queues, 1));
            
            Assert.Equal("serverName", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueuesArgumentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new WorkerManager(_storage.Object, Server, null, 1));

            Assert.Equal("queueNames", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenWorkerCountIsLessOrEqualToZero()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new WorkerManager(_storage.Object, Server, Queues, 0));

            Assert.Equal("workerCount", exception.ParamName);
        }

        [Fact(Timeout = 20 * 1000)]
        public void SmokeTest_With10Workers()
        {
            var manager = new WorkerManager(_storage.Object, Server, Queues, 10);
            manager.Dispose();
        }
    }
}
