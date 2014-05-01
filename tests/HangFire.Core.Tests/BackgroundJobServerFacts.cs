using System;
using System.Linq;
using HangFire.Server;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class BackgroundJobServerFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IServerComponentRunner> _runner;
        private readonly Mock<BackgroundJobServer> _serverMock;
        private readonly BackgroundJobServerOptions _options;

        public BackgroundJobServerFacts()
        {
            _storage = new Mock<JobStorage>();
            _options = new BackgroundJobServerOptions();

            _runner = new Mock<IServerComponentRunner>();
            _serverMock = new Mock<BackgroundJobServer>(_options, _storage.Object)
            {
                CallBase = true
            };
            _serverMock.Setup(x => x.GetServerRunner()).Returns(_runner.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobServer(null, _storage.Object));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobServer(_options, null));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact, GlobalLock(Reason = "Uses JobStorage.Current instance")]
        public void Ctor_HasDefaultValue_ForStorage()
        {
            JobStorage.Current = new Mock<JobStorage>().Object;
            Assert.DoesNotThrow(() => new BackgroundJobServer(_options));
        }

        [Fact, GlobalLock(Reason = "Uses JobStorage.Current instance")]
        public void Ctor_HasDefaultValue_ForOptions()
        {
            JobStorage.Current = new Mock<JobStorage>().Object;
            Assert.DoesNotThrow(() => new BackgroundJobServer());
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
            Assert.IsType<ServerCore>(((ServerComponentRunner) runner).Component);
        }

        [Fact]
        public void GetServerComponentsRunner_ContainsDefaultComponents()
        {
            // Arrange
            var server = CreateServer();

            // Act
            var runners = server.GetServerComponentsRunner();

            // Assert
            var componentTypes = runners.OfType<ServerComponentRunner>()
                .Select(x => x.Component)
                .Select(x => x.GetType())
                .ToArray();

            Assert.Contains(typeof(WorkerManager), componentTypes);
            Assert.Contains(typeof(ServerHeartbeat), componentTypes);
            Assert.Contains(typeof(ServerWatchdog), componentTypes);
            Assert.Contains(typeof(SchedulePoller), componentTypes);
        }

        [Fact]
        public void GetServerComponentsRunner_ContainsStorageComponents()
        {
            // Arrange
            var storageComponent = new Mock<IServerComponent>();
            _storage.Setup(x => x.GetComponents()).Returns(new[] { storageComponent.Object });

            var server = CreateServer();

            // Act
            var runners = server.GetServerComponentsRunner();

            // Assert
            var components = runners.OfType<ServerComponentRunner>()
                .Select(x => x.Component)
                .ToArray();

            Assert.Contains(storageComponent.Object, components);
        }

        private BackgroundJobServer CreateServer()
        {
            return new BackgroundJobServer(_options, _storage.Object);
        }
    }
}
