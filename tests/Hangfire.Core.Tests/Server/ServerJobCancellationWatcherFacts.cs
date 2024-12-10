using System;
using System.Collections.Concurrent;
using System.Threading;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ServerJobCancellationWatcherFacts : IDisposable
    {
        private readonly Mock<IStorageConnection> _connection;
        private readonly BackgroundProcessContextMock _context;
        private readonly TimeSpan _checkInterval;

        public ServerJobCancellationWatcherFacts()
        {
            _checkInterval = Timeout.InfiniteTimeSpan;

            _context = new BackgroundProcessContextMock();
            _context.StoppingTokenSource.Cancel();

            _connection = new Mock<IStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            ServerJobCancellationToken.AddServer(_context.ServerId);
        }

        public void Dispose()
        {
            ServerJobCancellationToken.RemoveServer(_context.ServerId);
        }

        [Fact]
        public void Execute_DelegatesCancellationToServerJobCancellationToken()
        {
            var token = new ServerJobCancellationToken(_connection.Object, "job-id", _context.ServerId, "1", _context.StoppedTokenSource.Token);
            Assert.False(token.ShutdownToken.IsCancellationRequested);

            _connection.Setup(x => x.GetStateData(It.IsAny<string>())).Returns((StateData)null);
            var watchdog = new ServerJobCancellationWatcher(_checkInterval);

            Assert.Throws<OperationCanceledException>(() => watchdog.Execute(_context.Object));

            _connection.Verify(x => x.GetStateData("job-id"));
            _connection.Verify(x => x.Dispose(), Times.Once);
            Assert.True(token.IsAborted);
        }
    }
}
