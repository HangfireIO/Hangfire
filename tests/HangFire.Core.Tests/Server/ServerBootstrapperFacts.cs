using System;
using System.Threading;
using HangFire.Server;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class ServerBootstrapperFacts
    {
        private const string ServerId = "server";

        private readonly ServerContext _context;
        private readonly Mock<JobStorage> _storage;
        private readonly Lazy<IServerSupervisor> _supervisorFactory;
        private readonly Mock<IStorageConnection> _connection;
        private readonly CancellationTokenSource _cts;
        private readonly Mock<IServerSupervisor> _supervisor;

        public ServerBootstrapperFacts()
        {
            _context = new ServerContext();
            _storage = new Mock<JobStorage>();
            _supervisor = new Mock<IServerSupervisor>();
            _supervisorFactory = new Lazy<IServerSupervisor>(() => _supervisor.Object);
            _connection = new Mock<IStorageConnection>();
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerBootstrapper(null, _context, _storage.Object, _supervisorFactory));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerBootstrapper(ServerId, null, _storage.Object, _supervisorFactory));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerBootstrapper(ServerId, _context, null, _supervisorFactory));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenSupervisorFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerBootstrapper(ServerId, _context, _storage.Object, null));

            Assert.Equal("supervisorFactory", exception.ParamName);
        }

        [Fact]
        public void Execute_AnnouncesServer()
        {
            var server = CreateServer();

            server.Execute(_cts.Token);

            _connection.Verify(x => x.AnnounceServer(ServerId, _context));
        }

        [Fact]
        public void Execute_GetsExactlyTwoConnections_AndClosesThem()
        {
            var server = CreateServer();
            
            server.Execute(_cts.Token);

            _storage.Verify(x => x.GetConnection(), Times.Exactly(2));
            _connection.Verify(x => x.Dispose(), Times.Exactly(2));
        }

        [Fact]
        public void Execute_StartsTheSupervisor()
        {
            var server = CreateServer();
            server.Execute(_cts.Token);

            _supervisor.Verify(x => x.Start());
        }

        [Fact]
        public void Execute_DisposesTheSupervisor()
        {
            var server = CreateServer();

            server.Execute(_cts.Token);

            _supervisor.Verify(x => x.Dispose());
        }

        [Fact]
        public void Execute_RemovesServerFromServersList()
        {
            var server = CreateServer();
            
            server.Execute(_cts.Token);

            _connection.Verify(x => x.RemoveServer(ServerId));
        }

        [Fact]
        public void Execute_RemovesServer_EvenWhenSupervisorThrowsAnException()
        {
            _supervisor.Setup(x => x.Dispose()).Throws<InvalidOperationException>();
            var server = CreateServer();

            Assert.Throws<InvalidOperationException>(() => server.Execute(_cts.Token));

            _connection.Verify(x => x.RemoveServer(It.IsAny<string>()));
        }

        private ServerBootstrapper CreateServer()
        {
            return new ServerBootstrapper(ServerId, _context, _storage.Object, _supervisorFactory);
        }
    }
}
