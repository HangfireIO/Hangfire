using System;
using System.Collections;
using System.Linq;
using HangFire.Server;
using HangFire.Server.Components;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class BackgroundJobServerFacts
    {
        private const int WorkerCount = 2;
        private static readonly string[] Queues = { "default" };

        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IServerComponentRunner> _runner;
        private readonly Mock<BackgroundJobServer2> _serverMock;

        public BackgroundJobServerFacts()
        {
            _storage = new Mock<JobStorage>();

            _runner = new Mock<IServerComponentRunner>();
            _serverMock = new Mock<BackgroundJobServer2>(WorkerCount, Queues, _storage.Object)
            {
                CallBase = true
            };
            _serverMock.Setup(x => x.GetServerRunner()).Returns(_runner.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenWorkerCountIsEqualToZeroOrNegative()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new BackgroundJobServer2(0, Queues, _storage.Object));

            Assert.Equal("workerCount", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueuesArrayIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobServer2(WorkerCount, null, _storage.Object));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobServer2(WorkerCount, Queues, null));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Start_StartsTheServerComponentRunner()
        {
            _serverMock.Object.Start();

            _runner.Verify(x => x.Start());
        }

        [Fact]
        public void Stop_StopsTheServerComponentRunner()
        {
            _serverMock.Object.Stop();

            _runner.Verify(x => x.Stop());
        }

        [Fact]
        public void Dispose_DisposesServerComponentRunner()
        {
            _serverMock.Object.Dispose();

            _runner.Verify(x => x.Dispose());
        }

        [Fact]
        public void GetServerRunner_ReturnsNonNullResult()
        {
            var server = CreateServer();

            var runner = server.GetServerRunner();

            Assert.NotNull(runner);
            Assert.IsType<JobServer2>(((ServerComponentRunner) runner).Component);
        }

        [Fact]
        public void GetServerComponentsRunner_ContainsDefaultComponents()
        {
            // Arrange
            var server = CreateServer();

            // Act
            var runners = server.GetServerComponentsRunner();

            // Assert
            Assert.True(runners.Select(x => x.GetType()).Contains(typeof(WorkerManager2)));

            var componentTypes = runners.OfType<ServerComponentRunner>()
                .Select(x => x.Component)
                .Select(x => x.GetType())
                .ToArray();

            Assert.Contains(typeof(ServerHeartbeat), componentTypes);
            Assert.Contains(typeof(ServerWatchdog2), componentTypes);
        }

        [Fact]
        public void GetServerComponentsRunner_ContainsStorageComponents()
        {
            // Arrange
            var storageComponent = new Mock<IServerComponent>();
            _storage.Setup(x => x.GetComponents2()).Returns(new[] { storageComponent.Object });

            var server = CreateServer();

            // Act
            var runners = server.GetServerComponentsRunner();

            // Assert
            var components = runners.OfType<ServerComponentRunner>()
                .Select(x => x.Component)
                .ToArray();

            Assert.Contains(storageComponent.Object, components);
        }

        private BackgroundJobServer2 CreateServer()
        {
            return new BackgroundJobServer2(WorkerCount, Queues, _storage.Object);
        }
    }
}
