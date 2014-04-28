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
        private readonly Lazy<IServerComponentRunner> _lazyRunner;
        private readonly Mock<IStorageConnection> _connection;
        private readonly CancellationToken _token;
        private readonly Mock<IServerComponentRunner> _runner;

        public JobServerFacts()
        {
            _context = new ServerContext();
            _storage = new Mock<JobStorage>();
            _runner = new Mock<IServerComponentRunner>();
            _lazyRunner = new Lazy<IServerComponentRunner>(() => _runner.Object);
            _connection = new Mock<IStorageConnection>();
            _token = new CancellationToken(true);

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobServer(null, _context, _storage.Object, _lazyRunner));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobServer(ServerId, null, _storage.Object, _lazyRunner));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobServer(ServerId, _context, null, _lazyRunner));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenRunnerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobServer(ServerId, _context, _storage.Object, null));

            Assert.Equal("runner", exception.ParamName);
        }

        [Fact]
        public void Execute_AnnouncesServer()
        {
            var server = CreateServer();

            server.Execute(_token);

            _connection.Verify(x => x.AnnounceServer(ServerId, _context));
        }

        [Fact]
        public void Execute_GetsExactlyTwoConnections_AndClosesThem()
        {
            var server = CreateServer();
            
            server.Execute(_token);

            _storage.Verify(x => x.GetConnection(), Times.Exactly(2));
            _connection.Verify(x => x.Dispose(), Times.Exactly(2));
        }

        [Fact]
        public void Execute_StartsTheRunner()
        {
            var server = CreateServer();
            server.Execute(_token);

            _runner.Verify(x => x.Start());
        }

        [Fact]
        public void Execute_DisposesTheRunner()
        {
            var server = CreateServer();

            server.Execute(_token);

            _runner.Verify(x => x.Dispose());
        }

        [Fact]
        public void Execute_RemovesServerFromServersList()
        {
            var server = CreateServer();
            
            server.Execute(_token);

            _connection.Verify(x => x.RemoveServer(ServerId));
        }

        [Fact]
        public void Execute_RemovesServer_EvenWhenRunnerThrowsAnException()
        {
            _runner.Setup(x => x.Dispose()).Throws<InvalidOperationException>();
            var server = CreateServer();

            Assert.Throws<InvalidOperationException>(() => server.Execute(_token));

            _connection.Verify(x => x.RemoveServer(It.IsAny<string>()));
        }

        private JobServer CreateServer()
        {
            return new JobServer(ServerId, _context, _storage.Object, _lazyRunner);
        }
    }
}
