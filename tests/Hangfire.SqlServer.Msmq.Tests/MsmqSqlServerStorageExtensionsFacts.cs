using System;
using System.Linq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Msmq.Tests
{
    public class MsmqSqlServerStorageExtensionsFacts
    {
        private readonly SqlServerStorage _storage;

        public MsmqSqlServerStorageExtensionsFacts()
        {
            _storage = new SqlServerStorage(
                @"Server=.\sqlexpress;Database=TheDatabase;Trusted_Connection=True;",
                new SqlServerStorageOptions { PrepareSchemaIfNecessary = false });
        }

        [Fact]
        public void UseMsmqQueues_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => MsmqSqlServerStorageExtensions.UseMsmqQueues(null, CleanMsmqQueueAttribute.PathPattern));
            
            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void UseMsmqQueues_AddsMsmqJobQueueProvider()
        {
            _storage.UseMsmqQueues(CleanMsmqQueueAttribute.PathPattern);

            var providerTypes = _storage.QueueProviders.Select(x => x.GetType());
            Assert.Contains(typeof(MsmqJobQueueProvider), providerTypes);
        }
    }
}
