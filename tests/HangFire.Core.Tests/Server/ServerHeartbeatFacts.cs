using System;
using System.Threading;
using HangFire.Server;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class ServerHeartbeatFacts
    {
        private const string ServerId = "server";
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IStorageConnection> _connection;
        private readonly CancellationToken _token;

        public ServerHeartbeatFacts()
        {
            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();
            _token = new CancellationToken(true);

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerHeartbeat(null, ServerId));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerHeartbeat(_storage.Object, null));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Execute_TakesAConnection_AndDisposesIt()
        {
            var server = CreateHeartbeat();

            server.Execute(_token);

            _storage.Verify(x => x.GetConnection(), Times.Once);
            _connection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Execute_UpdateServerHeartbeat()
        {
            var server = CreateHeartbeat();

            server.Execute(_token);

            _connection.Verify(x => x.Heartbeat(ServerId));
        }

        private ServerHeartbeat CreateHeartbeat()
        {
            return new ServerHeartbeat(_storage.Object, ServerId);
        }
    }
}
