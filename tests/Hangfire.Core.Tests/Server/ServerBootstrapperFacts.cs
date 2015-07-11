using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ServerBootstrapperFacts
    {
        private const string ServerId = "server";

        private readonly ServerContext _context;
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IStorageConnection> _connection;
        private readonly CancellationTokenSource _cts;
        private readonly List<IServerComponent> _components; 

        public ServerBootstrapperFacts()
        {
            _context = new ServerContext();
            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();
            _cts = new CancellationTokenSource();
            _components = new List<IServerComponent>();

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerBootstrapper(null, _context, _storage.Object, _components));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerBootstrapper(ServerId, null, _storage.Object, _components));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerBootstrapper(ServerId, _context, null, _components));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenComponentsCollection_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerBootstrapper(ServerId, _context, _storage.Object, null));

            Assert.Equal("components", exception.ParamName);
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
        public void Execute_StartsAllTheComponents_AndWaitsForThem()
        {
            // Arrange
            var component1 = CreateComponentMock();
            component1.Setup(x => x.Execute(It.IsAny<CancellationToken>())).Callback(() => Thread.Sleep(100));

            var component2 = CreateComponentMock();
            var server = CreateServer();

            // Act
            server.Execute(_cts.Token);

            // Assert
            component1.Verify(x => x.Execute(_cts.Token));
            component2.Verify(x => x.Execute(_cts.Token));
        }

        [Fact]
        public void Execute_RemovesServerFromServersList()
        {
            var server = CreateServer();
            
            server.Execute(_cts.Token);

            _connection.Verify(x => x.RemoveServer(ServerId));
        }

        private ServerBootstrapper CreateServer()
        {
            return new ServerBootstrapper(ServerId, _context, _storage.Object, _components);
        }

        private Mock<IServerComponent> CreateComponentMock()
        {
            var mock = new Mock<IServerComponent>();
            _components.Add(mock.Object);

            return mock;
        }
    }
}
