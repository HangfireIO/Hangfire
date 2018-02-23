using System;
using System.Collections.Generic;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ServerHeartbeatFacts
    {
        private readonly Mock<IStorageConnection> _connection;
        private readonly BackgroundProcessContextMock _context;

        public ServerHeartbeatFacts()
        {
            _context = new BackgroundProcessContextMock();
            _connection = new Mock<IStorageConnection>();

            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Execute_UpdateServerHeartbeat()
        {
            _connection.Setup(x => x.ServerPresent(_context.ServerId)).Returns(true);
            var serverContext = new ServerContext() { WorkerCount = 1 };
            var server = new ServerHeartbeat(TimeSpan.Zero, serverContext);

            server.Execute(_context.Object);

            _connection.Verify(x => x.Heartbeat(_context.ServerId));
            _connection.Verify(x => x.AnnounceServer(_context.ServerId, It.IsAny<ServerContext>()), Times.Never);
            _connection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Execute_AnnounceServerIfMissing()
        {
            _connection.Setup(x => x.ServerPresent(_context.ServerId)).Returns(false);
            var serverContext = new ServerContext(){WorkerCount = 1};
            var server = new ServerHeartbeat(TimeSpan.Zero, serverContext);

            server.Execute(_context.Object);

            _connection.Verify(x => x.Heartbeat(_context.ServerId));
            _connection.Verify(x => x.AnnounceServer(_context.ServerId, It.IsAny<ServerContext>()), Times.Once);
            _connection.Verify(x => x.Dispose(), Times.Once);
        }
    }
}
