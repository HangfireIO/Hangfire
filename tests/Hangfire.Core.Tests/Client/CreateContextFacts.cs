using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Profiling;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable ObjectCreationAsStatement
// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Client
{
    public class CreateContextFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Job _job;
        private readonly Mock<IState> _state;
        private readonly Dictionary<string, object> _parameters;
        private readonly Mock<ILog> _logger;
        private readonly Mock<IProfiler> _profiler;
        private readonly Dictionary<string, object> _items;

        public CreateContextFacts()
        {
            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();
            _job = Job.Create(() => Method());
            _state = new Mock<IState>();
            _parameters = new Dictionary<string, object>();
            _logger = new Mock<ILog>();
            _profiler = new Mock<IProfiler>();
            _items = new Dictionary<string, object>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new CreateContext(null, _connection.Object, _job, _state.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new CreateContext(_storage.Object, null, _job, _state.Object));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new CreateContext(_storage.Object, _connection.Object, null, _state.Object));

            Assert.Equal("job", exception.ParamName);
        }

        [Fact]
        public void Ctor_DoesNotThrowAnException_WhenStateIsNull()
        {
            // Does not throw
            var context = new CreateContext(_storage.Object, _connection.Object, _job, null);
            Assert.Null(context.InitialState);
        }

        [Fact]
        public void Ctor_UsesEmptyDictionary_WhenParametersAreNull()
        {
            // Does not throw
            var context = new CreateContext(_storage.Object, _connection.Object, _job, _state.Object, null);

            Assert.NotNull(context.Parameters);
            Assert.Empty(context.Parameters);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenLoggerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CreateContext(_storage.Object, _connection.Object, _job, _state.Object, _parameters, null));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProfilerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CreateContext(_storage.Object, _connection.Object, _job, _state.Object, _parameters, _logger.Object,
                    null, _items));

            Assert.Equal("profiler", exception.ParamName);
        }

        [Fact]
        public void Ctor_UsesEmptyDictionary_WhenItemsAreNull()
        {
            // Does not throw
            var context = new CreateContext(_storage.Object, _connection.Object, _job, _state.Object, _parameters, _logger.Object, _profiler.Object, null);

            Assert.NotNull(context.Items);
            Assert.Empty(context.Items);
        }

        [Fact]
        public void Ctor_CorrectlyInitializes_AllProperties()
        {
            var context = CreateContext();

            Assert.Same(_storage.Object, context.Storage);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Same(_job, context.Job);
            Assert.Same(_state.Object, context.InitialState);
            Assert.Same(_parameters, context.Parameters);
            Assert.Same(_logger.Object, context.Logger);
            Assert.Same(_profiler.Object, context.Profiler);
            Assert.Same(_items, context.Items);
        }

        [Fact]
        public void CopyCtor_CopiesItemsDictionary_FromTheGivenContext()
        {
            var context = CreateContext();
            var contextCopy = new CreateContext(context);

            Assert.Same(context.Storage, contextCopy.Storage);
            Assert.Same(context.Connection, contextCopy.Connection);
            Assert.Same(context.Job, contextCopy.Job);
            Assert.Same(context.InitialState, contextCopy.InitialState);
            Assert.Same(context.Parameters, contextCopy.Parameters);
            Assert.Same(context.Logger, contextCopy.Logger);
            Assert.Same(context.Profiler, contextCopy.Profiler);
            Assert.Same(context.Items, contextCopy.Items);
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void Method()
        {
        }

        private CreateContext CreateContext()
        {
            return new CreateContext(_storage.Object, _connection.Object, _job, _state.Object, _parameters, _logger.Object, _profiler.Object, _items);
        }
    }
}
