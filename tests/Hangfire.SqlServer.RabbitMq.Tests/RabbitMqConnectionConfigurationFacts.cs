using System;
using System.Linq;
using Hangfire.SqlServer.RabbitMQ;
using Xunit;

namespace Hangfire.SqlServer.RabbitMq.Tests
{
    public class RabbitMqConnectionConfigurationFacts
    {
        [Fact]
        public void Ctor_UsesDefaultHost_WithDefaultConstructor()
        {
            RabbitMqConnectionConfiguration conf = new RabbitMqConnectionConfiguration();

            Assert.Equal(RabbitMqConnectionConfiguration.DefaultHost, conf.HostName);
        }

        [Fact]
        public void Ctor_UsesDefaultPort_WithDefaultConstructor()
        {
            RabbitMqConnectionConfiguration conf = new RabbitMqConnectionConfiguration();

            Assert.Equal(RabbitMqConnectionConfiguration.DefaultPort, conf.Port);
        }

        [Fact]
        public void Ctor_UsesDefaultUserName_WithDefaultConstructor()
        {
            RabbitMqConnectionConfiguration conf = new RabbitMqConnectionConfiguration();

            Assert.Equal(RabbitMqConnectionConfiguration.DefaultUser, conf.Username);
        }

        [Fact]
        public void Ctor_UsesDefaultPassword_WithDefaultConstructor()
        {
            RabbitMqConnectionConfiguration conf = new RabbitMqConnectionConfiguration();

            Assert.Equal(RabbitMqConnectionConfiguration.DefaultPassword, conf.Password);
        }

        [Fact]
        public void Ctor_UsesDefaultVirtualHost_WithDefaultConstructor()
        {
            RabbitMqConnectionConfiguration conf = new RabbitMqConnectionConfiguration();

            Assert.Equal(RabbitMqConnectionConfiguration.DefaultVirtualHost, conf.VirtualHost);
        }

        [Fact]
        public void Ctor_ThrowsException_WhenUriIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RabbitMqConnectionConfiguration((Uri)null));

            Assert.Equal("uri", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsException_WhenHostIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RabbitMqConnectionConfiguration((string)null));

            Assert.Equal("host", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsException_WhenUsernameIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RabbitMqConnectionConfiguration(
                    RabbitMqConnectionConfiguration.DefaultHost,
                    RabbitMqConnectionConfiguration.DefaultPort,
                    null,
                    RabbitMqConnectionConfiguration.DefaultPassword));

            Assert.Equal("username", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsException_WhenPasswordIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RabbitMqConnectionConfiguration(
                    RabbitMqConnectionConfiguration.DefaultHost,
                    RabbitMqConnectionConfiguration.DefaultPort,
                    RabbitMqConnectionConfiguration.DefaultUser,
                    null));

            Assert.Equal("password", exception.ParamName);
        }
    }
}
