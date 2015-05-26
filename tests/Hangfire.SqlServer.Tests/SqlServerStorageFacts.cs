using System;
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

        [Fact, CleanDatabase]
        public void Ctor_InitializesDefaultJobQueueProvider_AndPassesCorrectOptions()
        {
            var storage = CreateStorage();
            var providers = storage.QueueProviders;

            var provider = (SqlServerJobQueueProvider)providers.GetProvider("default");

            Assert.Same(_options, provider.Options);
        }

        [Fact, CleanDatabase]
        public void GetConnection_ReturnsExistingConnection_WhenStorageUsesIt()
        {
            var connection = ConnectionUtils.CreateConnection();
            var storage = new SqlServerStorage(connection);

            using (var storageConnection = (SqlServerConnection) storage.GetConnection())
            {
                Assert.Same(connection, storageConnection.Connection);
                Assert.False(storageConnection.OwnsConnection);
            }
        }

        [Fact, CleanDatabase(IsolationLevel.ReadUncommitted)]
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
                Assert.True(connection.OwnsConnection);
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
