using System;
using System.Threading;
using HangFire.Server;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class ServerWatchdogFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IStorageConnection> _connection;
        private readonly ServerWatchdogOptions _options;
        private readonly CancellationToken _token;

        public ServerWatchdogFacts()
        {
            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();
            _options = new ServerWatchdogOptions
            {
                CheckInterval = Timeout.InfiniteTimeSpan // To check that it exits by cancellation token
            };
            _token = new CancellationToken(true);

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ServerWatchdog(null));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ServerWatchdog(_storage.Object, null));
        }

        [PossibleHangingFact]
        public void Execute_GetsConnectionAndReleasesIt()
        {
            var watchdog = CreateWatchdog();

            watchdog.Execute(_token);

            _storage.Verify(x => x.GetConnection(), Times.Once);
            _connection.Verify(x => x.Dispose(), Times.Once);
        }

        [PossibleHangingFact]
        public void Execute_DelegatesRemovalToStorageConnection()
        {
            _connection.Setup(x => x.RemoveTimedOutServers(It.IsAny<TimeSpan>())).Returns(1);
            var watchdog = CreateWatchdog();

            watchdog.Execute(_token);

            _connection.Verify(x => x.RemoveTimedOutServers(_options.ServerTimeout));
        }

        private ServerWatchdog CreateWatchdog()
        {
            return new ServerWatchdog(_storage.Object, _options);
        }
    }
}
