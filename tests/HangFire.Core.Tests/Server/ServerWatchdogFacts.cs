using System;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class ServerWatchdogFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IStorageConnection> _connection;

        public ServerWatchdogFacts()
        {
            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }
    }
}
