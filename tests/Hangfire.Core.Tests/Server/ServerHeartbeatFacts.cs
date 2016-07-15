using System;
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
            var server = new ServerHeartbeat(TimeSpan.Zero);

			server.Execute(_context.Object);

            _connection.Verify(x => x.Heartbeat(_context.ServerId));
            _connection.Verify(x => x.Dispose(), Times.Once);
        }
    }
}
