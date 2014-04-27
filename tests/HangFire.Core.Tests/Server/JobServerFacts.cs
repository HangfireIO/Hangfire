using System;
using System.Threading;
using HangFire.Server;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class JobServerFacts
    {
        private const string ServerId = "server";

        private readonly ServerContext _context;
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IServerComponentRunner> _runner;
        private readonly Mock<IStorageConnection> _connection;
        private readonly CancellationToken _token;

        public JobServerFacts()
        {
            _context = new ServerContext();
            _storage = new Mock<JobStorage>();
            _runner = new Mock<IServerComponentRunner>();
            _connection = new Mock<IStorageConnection>();
            _token = new CancellationToken(true);

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobServer2(null, _context, _storage.Object, _runner.Object));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobServer2(ServerId, null, _storage.Object, _runner.Object));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobServer2(ServerId, _context, null, _runner.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenRunnerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobServer2(ServerId, _context, _storage.Object, null));

            Assert.Equal("runner", exception.ParamName);
        }

        [Fact]
        public void Ctor_CreatesConnection_AnnouncesServer_AndClosesConnection()
        {
            CreateServer();

            _connection.Verify(x => x.AnnounceServer(ServerId, _context));
            _storage.Verify(x => x.GetConnection(), Times.Once);
            _connection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Start_StartsTheRunner()
        {
            var server = CreateServer();
            server.Start();

            _runner.Verify(x => x.Start());
        }

        [Fact]
        public void Stop_StopsTheRunner()
        {
            var server = CreateServer();
            server.Stop();

            _runner.Verify(x => x.Stop());
        }

        [Fact]
        public void Execute_TakesAConnection_AndDisposesIt()
        {
            var server = CreateServer();

            server.Execute(_token);

            _storage.Verify(x => x.GetConnection(), Times.Exactly(2)); // Ctor: 1, Execute: 1
            _connection.Verify(x => x.Dispose(), Times.Exactly(2));
        }

        [Fact]
        public void Execute_UpdateServerHeartbeat()
        {
            var server = CreateServer();

            server.Execute(_token);

            _connection.Verify(x => x.Heartbeat(ServerId));
        }

        [Fact]
        public void Dispose_DisposesTheRunner()
        {
            var server = CreateServer();

            server.Dispose();

            _runner.Verify(x => x.Dispose());
        }

        [Fact]
        public void Dispose_TakesAConnectionAndDisposesIt()
        {
            var server = CreateServer();

            server.Dispose();

            _storage.Verify(x => x.GetConnection(), Times.Exactly(2)); // Ctor: 1, Dispose: 1
            _connection.Verify(x => x.Dispose(), Times.Exactly(2));
        }

        [Fact]
        public void Dispose_RemovesServerFromServersList()
        {
            var server = CreateServer();
            
            server.Dispose();

            _connection.Verify(x => x.RemoveServer(ServerId));
        }

        [Fact]
        public void Dispose_RemovesServer_EvenWhenRunnerThrowsAnException()
        {
            _runner.Setup(x => x.Dispose()).Throws<InvalidOperationException>();
            var server = CreateServer();

            Assert.Throws<InvalidOperationException>(() => server.Dispose());

            _connection.Verify(x => x.RemoveServer(It.IsAny<string>()));
        }

        private JobServer2 CreateServer()
        {
            return new JobServer2(ServerId, _context, _storage.Object, _runner.Object);
        }
    }
}
