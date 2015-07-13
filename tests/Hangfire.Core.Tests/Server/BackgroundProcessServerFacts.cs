using System;
using System.Collections.Generic;
using Hangfire.Server;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class BackgroundProcessServerFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly List<IServerProcess> _processes;
        private readonly Dictionary<string, object> _properties;

        public BackgroundProcessServerFacts()
        {
            _storage = new Mock<JobStorage>();
            _processes = new List<IServerProcess>();
            _properties = new Dictionary<string, object>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessServer(null, _processes, _properties));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessesArgumentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessServer(_storage.Object, null, _properties));

            Assert.Equal("processes", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenPropertiesArgumentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessServer(_storage.Object, _processes, null));
            
            Assert.Equal("properties", exception.ParamName);
        }
    }
}
