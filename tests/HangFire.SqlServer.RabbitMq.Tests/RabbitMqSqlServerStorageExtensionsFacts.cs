using System;
using System.Linq;
using HangFire.SqlServer.RabbitMQ;
using RabbitMQ.Client;
using Xunit;

namespace HangFire.SqlServer.RabbitMq.Tests
{
    public class RabbitMqSqlServerStorageExtensionsFacts
    {
        private readonly SqlServerStorage _storage;

        public RabbitMqSqlServerStorageExtensionsFacts()
        {
            _storage = new SqlServerStorage(
                String.Empty,
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
            ConnectionFactory cfa = new ConnectionFactory();
            _storage.UseRabbitMq(conf => conf.HostName = "localhost");

            var providerTypes = _storage.QueueProviders.Select(x => x.GetType());
            Assert.Contains(typeof(RabbitMqJobQueueProvider), providerTypes);
        }
    }
}
