using System;
using System.Threading;
using Hangfire.Server;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class BackgroundProcessContextFacts
    {
        private readonly string _serverId = "server";
        private readonly Mock<JobStorage> _storage;
        private readonly CancellationTokenSource _cts;

        public BackgroundProcessContextFacts()
        {
            _storage = new Mock<JobStorage>();
            _cts = new CancellationTokenSource();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessContext(null, _storage.Object, _cts.Token));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessContext(_serverId, null, _cts.Token));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlyInitializes_AllTheProperties()
        {
            var context = new BackgroundProcessContext(_serverId, _storage.Object, _cts.Token);

            Assert.Equal(_serverId, context.ServerId);
            Assert.NotNull(context.Properties);
            Assert.Same(_storage.Object, context.Storage);
            Assert.Equal(_cts.Token, context.CancellationToken);
        }
    }
}
