using Moq;
using System;
using System.Data.Common;
using System.Linq;
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
            using (var connection = ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient))
            {
                var storage = new SqlServerStorage(connection);
                Assert.NotNull(storage);
            }
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

        [Fact, CleanDatabase]
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

#if NET452 || NET461
        [Fact, CleanDatabase]
        public void UseConnection_UsesSystemDataSqlClient_ByDefault_OnNet452Only()
        {
            var storage = CreateStorage();
            storage.UseConnection(null, connection =>
            {
                Assert.IsType<System.Data.SqlClient.SqlConnection>(connection);
            });
        }
#else
        [Fact, CleanDatabase]
        public void UseConnection_UsesMicrosoftDataSqlClient_ByDefault()
        {
            var storage = CreateStorage();
            storage.UseConnection(null, connection =>
            {
                Assert.IsType<Microsoft.Data.SqlClient.SqlConnection>(connection);
            });
        }
#endif

#if !NET452
        [Fact, CleanDatabase]
        public void UseConnection_UsesSystemDataSqlClient_WhenSqlClientFactoryIsSet()
        {
            _options.SqlClientFactory = System.Data.SqlClient.SqlClientFactory.Instance;
            var storage = CreateStorage();
            storage.UseConnection(null, connection =>
            {
                Assert.IsType<System.Data.SqlClient.SqlConnection>(connection);
            });
        }
#endif

        [Fact, CleanDatabase]
        public void GetComponents_ReturnsAllNeededComponents()
        {
            var storage = CreateStorage();

            var components = storage.GetComponents();

            var componentTypes = components.Select(x => x.GetType()).ToArray();
            Assert.Contains(typeof(ExpirationManager), componentTypes);
        }

        [Fact, CleanDatabase]
        public void HasFeature_Connection_GetUtcDateTime_ReturnsTrue()
        {
            var storage = CreateStorage();

            var result = storage.HasFeature("Connection.GetUtcDateTime");

            Assert.True(result);
        }

        [Fact, CleanDatabase]
        public void HasFeature_Connection_GetSetContains_ReturnsTrue()
        {
            var storage = CreateStorage();

            var result = storage.HasFeature("Connection.GetSetContains");

            Assert.True(result);
        }

        [Fact, CleanDatabase]
        public void HasFeature_Connection_GetSetCount_Limited_ReturnsTrue()
        {
            var storage = CreateStorage();

            var result = storage.HasFeature("Connection.GetSetCount.Limited");

            Assert.True(result);
        }

        [Fact, CleanDatabase]
        public void HasFeature_Job_Queue_ReturnsTrue()
        {
            var storage = CreateStorage();

            var result = storage.HasFeature("Job.Queue");

            Assert.True(result);
        }

        [Fact, CleanDatabase]
        public void HasFeature_Transaction_AcquireDistributedLock_ReturnsTrue()
        {
            var storage = CreateStorage();

            var result = storage.HasFeature("Transaction.AcquireDistributedLock");

            Assert.True(result);
        }

        private SqlServerStorage CreateStorage()
        {
            return new SqlServerStorage(
                ConnectionUtils.GetConnectionString(),
                _options);
        }
    }
}
