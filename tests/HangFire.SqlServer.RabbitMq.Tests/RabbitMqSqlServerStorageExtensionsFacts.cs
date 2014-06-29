using System;
using System.Linq;
using Hangfire.SqlServer.RabbitMQ;
using Xunit;

namespace Hangfire.SqlServer.RabbitMq.Tests
{
    public class RabbitMqSqlServerStorageExtensionsFacts
    {
        private readonly SqlServerStorage _storage;

        public RabbitMqSqlServerStorageExtensionsFacts()
        {
            _storage = new SqlServerStorage(
                @"Server=.\sqlexpress;Database=TheDatabase;Trusted_Connection=True;",
                new SqlServerStorageOptions { PrepareSchemaIfNecessary = false });
        }

        [Fact]
        public void UseRabbitMq_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => RabbitMqSqlServerStorageExtensions.UseRabbitMq(null, conf => conf.HostName = "localhost"));
            
            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void UseRabbitMq_AddsMsmqJobQueueProvider()
        {
            _storage.UseRabbitMq(conf => conf.HostName = "localhost");

            var providerTypes = _storage.QueueProviders.Select(x => x.GetType());
            Assert.Contains(typeof(RabbitMqJobQueueProvider), providerTypes);
        }
    }
}
