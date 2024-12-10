using System;
using System.Threading;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ServerWatchdogFacts
    {
        private readonly Mock<IStorageConnection> _connection;
        private readonly BackgroundProcessContextMock _context;
        private readonly TimeSpan _checkInterval;
        private readonly TimeSpan _serverTimeout;

        public ServerWatchdogFacts()
        {
            _checkInterval = Timeout.InfiniteTimeSpan;
            _serverTimeout = TimeSpan.FromSeconds(5);

            _context = new BackgroundProcessContextMock();
            _context.StoppingTokenSource.Cancel();

            _connection = new Mock<IStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Execute_DelegatesRemovalToStorageConnection()
        {
            _connection.Setup(x => x.RemoveTimedOutServers(It.IsAny<TimeSpan>())).Returns(1);
            var watchdog = new ServerWatchdog(_checkInterval, _serverTimeout);

            Assert.Throws<OperationCanceledException>(() => watchdog.Execute(_context.Object));

            _connection.Verify(x => x.RemoveTimedOutServers(_serverTimeout));
            _connection.Verify(x => x.Dispose(), Times.Once);
        }
    }
}
