namespace Hangfire.Core.Tests
{
    public class BackgroundJobServerFacts
    {
        /*private readonly Mock<JobStorage> _storage;
        private readonly Mock<IServerSupervisor> _supervisor;
        private readonly Mock<BackgroundJobServer> _serverMock;
        private readonly BackgroundJobServerOptions _options;

        public BackgroundJobServerFacts()
        {
            _storage = new Mock<JobStorage>();
            _options = new BackgroundJobServerOptions();

            _supervisor = new Mock<IServerSupervisor>();
            _serverMock = new Mock<BackgroundJobServer>(_options, _storage.Object)
            {
                CallBase = true
            };
            _serverMock.Setup(x => x.GetBootstrapTask()).Returns(_supervisor.Object);
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
            Assert.DoesNotThrow(() => StartServer(
                () => new BackgroundJobServer(_options)));
        }

        [Fact]
        public void Ctor_HasDefaultValue_ForOptions()
        {
            Assert.DoesNotThrow(() => StartServer(
                () => new BackgroundJobServer(_storage.Object)));
        }

        [Fact, GlobalLock(Reason = "Uses JobStorage.Current instance")]
        public void Ctor_HasDefaultValues_ForAllParameters()
        {
            JobStorage.Current = new Mock<JobStorage>().Object;
            Assert.DoesNotThrow(() => StartServer(
                () => new BackgroundJobServer()));
        }

        [Fact]
        public void Ctor_StartsTheBootstrapSupervisor()
        {
            var instance = _serverMock.Object;
            _supervisor.Verify(x => x.Start());
        }

        [Fact]
        public void Dispose_DisposesBootstrapSupervisor()
        {
            _serverMock.Object.Dispose();

            _supervisor.Verify(x => x.Dispose());
        }

        [Fact]
        public void GetBootstrapSupervisor_ReturnsBootstrapper_WrappedWithAutomaticRetry()
        {
            // Arrange
            var server = CreateServer();

            // Act
            var supervisor = server.GetBootstrapTask();

            // Assert
            Assert.NotNull(supervisor);

            var wrapper = ((ServerSupervisor) supervisor).Component;

            Assert.IsType<AutomaticRetryServerComponentWrapper>(wrapper);
            Assert.IsType<ServerBootstrapper>(((AutomaticRetryServerComponentWrapper)wrapper).InnerComponent);
        }

        [Fact]
        public void GetSupervisors_ContainsDefaultComponents_WrappedTo_AutomaticRetryServerComponentWrapper()
        {
            // Arrange
            var server = CreateServer();

            // Act
            var supervisors = server.GetSupervisors();

            // Assert
            var componentTypes = supervisors.OfType<ServerSupervisor>()
                .Select(x => x.Component)
                .Cast<AutomaticRetryServerComponentWrapper>()
                .Select(x => x.InnerComponent)
                .Select(x => x.GetType())
                .ToArray();

            Assert.Contains(typeof(Worker), componentTypes);
            Assert.Contains(typeof(ServerHeartbeat), componentTypes);
            Assert.Contains(typeof(ServerWatchdog), componentTypes);
            Assert.Contains(typeof(DelayedJobSchedulerFacts), componentTypes);
        }

        [Fact]
        public void GetSupervisors_ContainsStorageComponents_WrappedTo_AutomaticRetryServerComponentWrapper()
        {
            // Arrange
            var storageComponent = new Mock<IServerComponent>();
            _storage.Setup(x => x.GetComponents()).Returns(new[] { storageComponent.Object });

            var server = CreateServer();

            // Act
            var supervisors = server.GetSupervisors();

            // Assert
            var components = supervisors.OfType<ServerSupervisor>()
                .Select(x => x.Component)
                .Cast<AutomaticRetryServerComponentWrapper>()
                .Select(x => x.InnerComponent)
                .ToArray();

            Assert.Contains(storageComponent.Object, components);
        }

        private BackgroundJobServer CreateServer()
        {
            return new BackgroundJobServer(_options, _storage.Object);
        }

        private void StartServer(Func<BackgroundJobServer> createFunc)
        {
            using (createFunc()) { }
        }*/
    }
}
