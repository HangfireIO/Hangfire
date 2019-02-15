using System;
using System.Threading;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ServerJobCancellationWatcherFacts
    {
        private readonly Mock<IStorageConnection> _connection;
        private readonly BackgroundProcessContextMock _context;
        private readonly TimeSpan _checkInterval;

        public ServerJobCancellationWatcherFacts()
        {
            _checkInterval = Timeout.InfiniteTimeSpan;

            _context = new BackgroundProcessContextMock();
            _context.StoppingTokenSource.Cancel();
            _context.StoppedTokenSource.Cancel();

            _connection = new Mock<IStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Execute_DelegatesCancellationToServerJobCancellationToken()
        {
            var token = new ServerJobCancellationToken(_connection.Object, "job-id", _context.ServerId, "1", _context.StoppedTokenSource.Token);

            _connection.Setup(x => x.GetStateData(It.IsAny<string>())).Returns((StateData)null);
            var watchdog = new ServerJobCancellationWatcher(_checkInterval);

            watchdog.Execute(_context.Object);

            _connection.Verify(x => x.GetStateData("job-id"));
            _connection.Verify(x => x.Dispose(), Times.Once);
            Assert.True(token.IsAborted);
        }
    }
}
