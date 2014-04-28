using System;
using System.Linq;
using System.Transactions;
using HangFire.SqlServer.Components;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class StorageFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionStringIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerStorage(null));

            Assert.Equal("connectionString", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerStorage("hello", null));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact, CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetMonitoringApi_ReturnsNonNullInstance()
        {
            var storage = CreateStorage();
            using (var api = storage.GetMonitoringApi())
            {
                Assert.NotNull(api);
            }
        }

        [Fact, CleanDatabase]
        public void GetConnection_ReturnsNonNullInstance()
        {
            var storage = CreateStorage();
            using (var connection = storage.GetConnection())
            {
                Assert.NotNull(connection);
            }
        }

        [Fact]
        public void GetComponents_ReturnsAllNeededComponents()
        {
            var storage = CreateStorage();

            var components = storage.GetComponents();

            var componentTypes = components.Select(x => x.GetType()).ToArray();
            Assert.Contains(typeof(ExpirationManager), componentTypes);
        }

        private static SqlServerStorage CreateStorage()
        {
            return new SqlServerStorage(
                ConnectionUtils.GetConnectionString(),
                new SqlServerStorageOptions { PrepareSchemaIfNecessary = false });
        }
    }
}
