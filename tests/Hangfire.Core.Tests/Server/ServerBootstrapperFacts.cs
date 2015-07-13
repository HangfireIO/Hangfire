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
        /*private readonly string[] _queues = { "queue" };
        private readonly BackgroundProcessContextMock _context;
        private readonly Mock<IStorageConnection> _connection;
        private readonly List<IServerProcess> _processes;

        public ServerBootstrapperFacts()
        {
            _context = new BackgroundProcessContextMock();
            _context.Object.Properties.Add("Queues", _queues);

            _connection = new Mock<IStorageConnection>();
            _processes = new List<IServerProcess>();

            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessesCollection_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerBootstrapper(null));

            Assert.Equal("processes", exception.ParamName);
        }

        [Fact]
        public void Execute_AnnouncesServer()
        {
            var server = CreateServer();

            server.Execute(_context.Object);

            _connection.Verify(x => x.AnnounceServer(
                _context.ServerId, 
                It.Is<ServerContext>(y => y.Queues == _queues)));
        }

        [Fact]
        public void Execute_GetsExactlyTwoConnections_AndClosesThem()
        {
            var server = CreateServer();
            
            server.Execute(_context.Object);

            _context.Storage.Verify(x => x.GetConnection(), Times.Exactly(2));
            _connection.Verify(x => x.Dispose(), Times.Exactly(2));
        }

        [Fact]
        public void Execute_StartsAllTheComponents_AndWaitsForThem()
        {
            // Arrange
            var component1 = CreateProcessMock<IServerComponent>();
            component1.Setup(x => x.Execute(It.IsAny<CancellationToken>())).Callback(() => Thread.Sleep(100));

            var component2 = CreateProcessMock<IBackgroundProcess>();
            var server = CreateServer();

            // Act
            server.Execute(_context.Object);

            // Assert
            component1.Verify(x => x.Execute(_context.CancellationTokenSource.Token));
            component2.Verify(x => x.Execute(_context.Object));
        }

        [Fact]
        public void Execute_RemovesServerFromServersList()
        {
            var server = CreateServer();
            
            server.Execute(_context.Object);

            _connection.Verify(x => x.RemoveServer(_context.ServerId));
        }

        private ServerBootstrapper CreateServer()
        {
            return new ServerBootstrapper(_processes);
        }

        private Mock<T> CreateProcessMock<T>()
            where T : class, IServerProcess
        {
            var mock = new Mock<T>();
            _processes.Add(mock.Object);

            return mock;
        }*/
    }
}
