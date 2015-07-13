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
        private readonly ServerWatchdogOptions _options;
        private readonly BackgroundProcessContextMock _context;

        public ServerWatchdogFacts()
        {
            
            _options = new ServerWatchdogOptions
            {
                CheckInterval = Timeout.InfiniteTimeSpan // To check that it exits by cancellation token
            };

            _context = new BackgroundProcessContextMock();
            _context.CancellationTokenSource.Cancel();

            _connection = new Mock<IStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsArgumentIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ServerWatchdog(null));
        }

        [PossibleHangingFact]
        public void Execute_DelegatesRemovalToStorageConnection()
        {
            _connection.Setup(x => x.RemoveTimedOutServers(It.IsAny<TimeSpan>())).Returns(1);
            var watchdog = new ServerWatchdog(_options);

			watchdog.Execute(_context.Object);

            _connection.Verify(x => x.RemoveTimedOutServers(_options.ServerTimeout));
            _connection.Verify(x => x.Dispose(), Times.Once);
        }
    }
}
