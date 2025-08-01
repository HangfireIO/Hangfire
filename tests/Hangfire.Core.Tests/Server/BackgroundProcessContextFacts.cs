using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.Logging;
using Hangfire.Profiling;
using Hangfire.Server;
using Moq;
using Xunit;
#pragma warning disable 618

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Server
{
    public class BackgroundProcessContextFacts
    {
        private readonly string _serverId = "server";
        private readonly Mock<JobStorage> _storage;
        private readonly Dictionary<string, object> _properties;
        private readonly Mock<ILog> _logger;
        private readonly Guid _executionId;
        private readonly CancellationTokenSource _cts;

        public BackgroundProcessContextFacts()
        {
            _storage = new Mock<JobStorage>();
            _properties = new Dictionary<string, object> {{"key", "value"}};
            _logger = new Mock<ILog>();
            _executionId = Guid.NewGuid();
            _cts = new CancellationTokenSource();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessContext(null, _storage.Object, _properties, _cts.Token));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessContext(_serverId, null, _properties, _cts.Token));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenPropertiesArgumentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessContext(_serverId, _storage.Object, null, _cts.Token));

            Assert.Equal("properties", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenLoggerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessContext(_serverId, _storage.Object, _properties, null, _executionId, _cts.Token, _cts.Token, _cts.Token));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlyInitializes_AllTheProperties()
        {
            var stoppingCts = new CancellationTokenSource();
            var stoppedCts = new CancellationTokenSource();
            var shutdownCts = new CancellationTokenSource();

            var context = new BackgroundProcessContext(_serverId, _storage.Object, _properties, _logger.Object, _executionId, stoppingCts.Token, stoppedCts.Token, shutdownCts.Token);

            Assert.Equal(_serverId, context.ServerId);
            Assert.Same(_storage.Object, context.Storage);
            Assert.True(_properties.SequenceEqual(context.Properties));
            Assert.Same(_logger.Object, context.Logger);
            Assert.Equal(_executionId, context.ExecutionId);
            Assert.Equal(stoppingCts.Token, context.CancellationToken);
            Assert.Equal(stoppingCts.Token, context.CancellationToken);
            Assert.Equal(stoppingCts.Token, context.StoppingToken);
            Assert.Equal(stoppedCts.Token, context.StoppedToken);
            Assert.Equal(shutdownCts.Token, context.ShutdownToken);
            Assert.IsType<SlowLogProfiler>(context.Profiler);
        }
    }
}
