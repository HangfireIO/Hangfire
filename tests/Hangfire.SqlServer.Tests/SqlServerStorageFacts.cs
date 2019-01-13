using Moq;
using System;
using System.Data.Common;
using System.Linq;
using System.Transactions;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerStorageFacts
    {
        private readonly SqlServerStorageOptions _options;

        public SqlServerStorageFacts()
        {
            _options = new SqlServerStorageOptions { PrepareSchemaIfNecessary = false };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionStringIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerStorage((string)null));

            Assert.Equal("nameOrConnectionString", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerStorage("hello", null));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact, CleanDatabase]
        public void Ctor_CanCreateSqlServerStorage_WithExistingConnection()
        {
            var connection = ConnectionUtils.CreateConnection();
            var storage = new SqlServerStorage(connection);

            Assert.NotNull(storage);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionfactoryIsNull()
        {
            Func<DbConnection> connectionFactory = null;

            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerStorage(connectionFactory));

            Assert.Equal("connectionFactory", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull_WithConnectionFactory()
        {
            Func<DbConnection> connectionFactory = ConnectionUtils.CreateConnection;
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerStorage(connectionFactory, null));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact, CleanDatabase]
        public void CreateAndOpenConnection_UsesConnectionFactory()
        {
            var mockConnectionFactory = new Mock<Func<DbConnection>>();
            var expectedConnection = ConnectionUtils.CreateConnection();
            mockConnectionFactory.Setup(x => x()).Returns(expectedConnection);
            var storage = new SqlServerStorage(mockConnectionFactory.Object);
            var actualConnection = storage.CreateAndOpenConnection();

            mockConnectionFactory.Verify(x => x(), Times.Once());
            Assert.Equal(actualConnection, expectedConnection);
        }

        [Fact, CleanDatabase(isolationLevel: IsolationLevel.ReadUncommitted)]
        public void GetMonitoringApi_ReturnsNonNullInstance()
        {
            var storage = CreateStorage();
            var api = storage.GetMonitoringApi();
            Assert.NotNull(api);
        }

        [Fact, CleanDatabase]
        public void GetConnection_ReturnsNonNullInstance()
        {
            var storage = CreateStorage();
            using (var connection = (SqlServerConnection)storage.GetConnection())
            {
                Assert.NotNull(connection);
            }
        }

        [Fact, CleanDatabase]
        public void GetComponents_ReturnsAllNeededComponents()
        {
            var storage = CreateStorage();

            var components = storage.GetComponents();

            var componentTypes = components.Select(x => x.GetType()).ToArray();
            Assert.Contains(typeof(ExpirationManager), componentTypes);
        }

        private SqlServerStorage CreateStorage()
        {
            return new SqlServerStorage(
                ConnectionUtils.GetConnectionString(),
                _options);
        }
    }
}
