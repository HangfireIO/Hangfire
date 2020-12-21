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

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Ctor_CanCreateSqlServerStorage_WithExistingConnection(bool useMicrosoftDataSqlClient)
        {
            var connection = ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient);
            var storage = new SqlServerStorage(connection);

            Assert.NotNull(storage);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionFactoryIsNull()
        {
            Func<DbConnection> connectionFactory = null;

            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerStorage(connectionFactory));

            Assert.Equal("connectionFactory", exception.ParamName);
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull_WithConnectionFactory(bool useMicrosoftDataSqlClient)
        {
            Func<DbConnection> connectionFactory = () => ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient);
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerStorage(connectionFactory, null));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void CreateAndOpenConnection_UsesConnectionFactory()
        {
            var connection = new Mock<DbConnection>();
            var storage = new SqlServerStorage(() => connection.Object, _options);

            Assert.Same(connection.Object, storage.CreateAndOpenConnection());
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
